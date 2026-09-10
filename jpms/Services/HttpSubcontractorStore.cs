using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Services;

public sealed class HttpSubcontractorStore : ISubcontractorStore
{
    // Matches the server-side cap in UploadComplianceDocumentFileEndpoint.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly SubcontractorsReadModel readModel;
    private readonly TradesReadModel tradesReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    // Compliance documents per subcontractor, cached so render-time reads never block on async
    // (which deadlocks on WebAssembly). Saving a document invalidates its subcontractor.
    private readonly AsyncQueryCache<string, IReadOnlyList<ComplianceDocument>> compliance;

    // Company contacts per record, cached for the same render-time reason. Upserting or removing
    // a contact invalidates its record.
    private readonly AsyncQueryCache<string, IReadOnlyList<CompanyContact>> contacts;

    public HttpSubcontractorStore(SubcontractorsReadModel readModel, TradesReadModel tradesReadModel, IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.readModel = readModel;
        this.tradesReadModel = tradesReadModel;
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
        readModel.OnChanged += () => OnChange?.Invoke();
        tradesReadModel.OnChanged += () => OnChange?.Invoke();
        compliance = new((id, ct) => queries.AskAsync(new ListComplianceDocumentsForSubcontractor(id), ct), () => OnChange?.Invoke());
        contacts = new((id, ct) => queries.AskAsync(new ListCompanyContacts(id), ct), () => OnChange?.Invoke());
    }

    public event Action? OnChange;

    public bool IsLoaded => readModel.Current is not null;

    public IReadOnlyList<Subcontractor> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<Subcontractor>();
    }

    public Subcontractor? Find(string subcontractorId) =>
        All().FirstOrDefault(sub => string.Equals(sub.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Trade> Trades()
    {
        if (tradesReadModel.Current is null) _ = tradesReadModel.RefreshAsync(CancellationToken.None);
        return tradesReadModel.Current ?? Array.Empty<Trade>();
    }

    public bool TradesLoaded => tradesReadModel.Current is not null;

    public async Task<Trade> AddTradeAsync(string name)
    {
        var trade = await commands.SendAsync(new AddTrade(name), CancellationToken.None);
        await tradesReadModel.RefreshAsync(CancellationToken.None);
        return trade;
    }

    public async Task SetTradesAsync(string subcontractorId, IReadOnlyList<string> tradeIds)
    {
        var sub = Find(subcontractorId)
            ?? throw new InvalidOperationException($"Subcontractor {subcontractorId} not found.");
        await commands.SendAsync(new UpdateSubcontractor(
            sub.SubcontractorId, sub.CompanyName, tradeIds, sub.ContactName, sub.ContactEmail,
            sub.ContactPhone, sub.CisStatus), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    // Company name + contact details + payment terms, preserving the record's trades, CIS
    // status and every other field. Renaming to match the Xero supplier name is what lines
    // invoices up on the WO Allocation tab.
    public async Task UpdateDetailsAsync(string subcontractorId, string companyName,
        string contactName, string contactEmail, string contactPhone, int paymentTermsDays,
        string addressLine, string town, string county, string postcode)
    {
        var sub = Find(subcontractorId)
            ?? throw new InvalidOperationException($"Subcontractor {subcontractorId} not found.");
        await commands.SendAsync(new UpdateSubcontractor(
            sub.SubcontractorId, companyName.Trim(), TradeIds(sub), contactName.Trim(),
            contactEmail.Trim(), contactPhone.Trim(), sub.CisStatus,
            PaymentTermsDays: paymentTermsDays,
            AddressLine: addressLine.Trim(), Town: town.Trim(),
            County: county.Trim(), Postcode: postcode.Trim()), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    public Subcontractor Upsert(Subcontractor subcontractor)
    {
        if (string.IsNullOrEmpty(subcontractor.SubcontractorId))
            _ = AddAsync(subcontractor);
        else _ = UpdateAsync(subcontractor);
        return subcontractor;
    }

    public IReadOnlyList<ComplianceDocument> ComplianceFor(string subcontractorId) =>
        compliance.Get(subcontractorId, Array.Empty<ComplianceDocument>());

    public void SaveCompliance(ComplianceDocument document) => _ = SaveComplianceAsync(document);

    private async Task SaveComplianceAsync(ComplianceDocument document)
    {
        await commands.SendAsync(
            new UploadComplianceDocument(document.SubcontractorId, document.Kind, document.FileName, document.ExpiresAt),
            CancellationToken.None);
        compliance.Invalidate(document.SubcontractorId);
    }

    public async Task UploadComplianceFileAsync(
        string subcontractorId, string kind, DateTimeOffset? expiresAt, IBrowserFile file,
        CancellationToken cancellationToken, decimal? publicLiabilityCover = null)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);
        content.Add(new StringContent(kind), "kind");
        if (expiresAt is not null) content.Add(new StringContent(expiresAt.Value.ToString("O")), "expiresAt");
        if (publicLiabilityCover is not null)
            content.Add(new StringContent(publicLiabilityCover.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "publicLiabilityCover");

        var response = await httpClient.PostAsync(
            $"api/subcontractors/{subcontractorId}/compliance/file", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Surface the server's message (e.g. a storage error) rather than a bare status code.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        // Committed — invalidate so the compliance list refetches and re-renders with the new version.
        compliance.Invalidate(subcontractorId);
    }

    private async Task AddAsync(Subcontractor sub)
    {
        await commands.SendAsync(new AddSubcontractorToDirectory(sub.CompanyName, TradeIds(sub), sub.ContactName, sub.ContactEmail, sub.ContactPhone, sub.CisStatus,
            sub.Category, sub.MobileNumber, sub.Town, sub.County, sub.Website, sub.PaymentTermsDays,
            AddressLine: sub.AddressLine, Postcode: sub.Postcode), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task UpdateAsync(Subcontractor sub)
    {
        await commands.SendAsync(new UpdateSubcontractor(sub.SubcontractorId, sub.CompanyName, TradeIds(sub), sub.ContactName, sub.ContactEmail, sub.ContactPhone, sub.CisStatus,
            PaymentTermsDays: sub.PaymentTermsDays,
            AddressLine: sub.AddressLine, Town: sub.Town, County: sub.County, Postcode: sub.Postcode), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    public async Task RecordCisVerificationAsync(RecordCisVerification command)
    {
        await commands.SendAsync(command, CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    public async Task SetComplianceDocumentDetailsAsync(SetComplianceDocumentDetails command, CancellationToken cancellationToken)
    {
        await commands.SendAsync(command, cancellationToken);
        // Committed — invalidate so the record page's list re-reads the corrected version.
        compliance.Invalidate(command.SubcontractorId);
    }

    // ---- Xero import + consolidation ----

    public Task<XeroSuppliersSnapshot> FetchXeroSuppliersAsync(bool force = false) =>
        queries.AskAsync(new ListXeroSuppliers(force), CancellationToken.None);

    public async Task<Subcontractor> ImportFromXeroAsync(string xeroContactId)
    {
        var imported = await commands.SendAsync(new ImportXeroSupplier(xeroContactId), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
        return imported;
    }

    public async Task<Subcontractor> LinkToXeroAsync(string subcontractorId, string xeroContactId, bool pullDetailsFromXero = false)
    {
        var linked = await commands.SendAsync(new LinkDirectoryRecordToXeroContact(subcontractorId, xeroContactId, pullDetailsFromXero), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
        if (pullDetailsFromXero) contacts.Invalidate(subcontractorId);
        return linked;
    }

    public Task<XeroContactPushPreview> PreviewXeroContactPushAsync(string subcontractorId) =>
        queries.AskAsync(new PreviewXeroContactPush(subcontractorId), CancellationToken.None);

    public Task<XeroContactPushOutcome> PushContactsToXeroAsync(string subcontractorId) =>
        commands.SendAsync(new PushDirectoryContactsToXeroContact(subcontractorId), CancellationToken.None);

    public async Task<Subcontractor> UnlinkFromXeroAsync(string subcontractorId, string xeroContactId)
    {
        var unlinked = await commands.SendAsync(new UnlinkDirectoryRecordFromXeroContact(subcontractorId, xeroContactId), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
        return unlinked;
    }

    public async Task<Subcontractor> ConsolidateAsync(ConsolidateDirectoryRecords command)
    {
        var master = await commands.SendAsync(command, CancellationToken.None);
        // The merged-away records' cached contacts AND compliance documents are stale — the
        // server moved their rows to the master.
        contacts.Invalidate(command.MasterSubcontractorId);
        compliance.Invalidate(command.MasterSubcontractorId);
        foreach (var mergedId in command.MergedSubcontractorIds)
        {
            contacts.Invalidate(mergedId);
            compliance.Invalidate(mergedId);
        }
        await readModel.RefreshAsync(CancellationToken.None);
        return master;
    }

    // ---- Company contacts ----

    public IReadOnlyList<CompanyContact> ContactsFor(string subcontractorId) =>
        contacts.Get(subcontractorId, Array.Empty<CompanyContact>());

    public bool ContactsLoadedFor(string subcontractorId) => contacts.Has(subcontractorId);

    public async Task UpsertContactAsync(UpsertCompanyContact command)
    {
        await commands.SendAsync(command, CancellationToken.None);
        contacts.Invalidate(command.SubcontractorId);
    }

    public async Task RemoveContactAsync(string subcontractorId, string companyContactId)
    {
        await commands.SendAsync(new RemoveCompanyContact(subcontractorId, companyContactId), CancellationToken.None);
        contacts.Invalidate(subcontractorId);
    }

    private static IReadOnlyList<string> TradeIds(Subcontractor sub) =>
        sub.Trades.Select(trade => trade.TradeId).ToList();
}
