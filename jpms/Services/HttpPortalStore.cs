using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Features.Portal;

namespace Jewel.JPMS.Services;

public sealed class HttpPortalStore : IPortalStore
{
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly PortalReadModel readModel;
    private readonly PortalWorkOrdersReadModel workOrdersReadModel;
    private readonly PortalVariationRequestsReadModel variationRequestsReadModel;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;
    private bool hasFetched;
    private Task? inFlight;

    public HttpPortalStore(
        PortalReadModel readModel, PortalWorkOrdersReadModel workOrdersReadModel,
        PortalVariationRequestsReadModel variationRequestsReadModel, ICommandSender commands, HttpClient httpClient)
    {
        this.readModel = readModel;
        this.workOrdersReadModel = workOrdersReadModel;
        this.variationRequestsReadModel = variationRequestsReadModel;
        this.commands = commands;
        this.httpClient = httpClient;
        readModel.OnChanged += () => OnChange?.Invoke();
        workOrdersReadModel.OnChanged += () => OnChange?.Invoke();
        variationRequestsReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public bool IsLoaded => readModel.Current is not null || hasFetched;

    public SubcontractorPortalRecord? MyRecord()
    {
        // Fetch at most once from a render-time read to avoid render → fetch → render loops
        // (CLAUDE.md convention); revalidation happens via Refresh() from OnInitializedAsync.
        if (readModel.Current is null && !hasFetched) _ = FetchAsync();
        return readModel.Current;
    }

    public IReadOnlyList<PortalWorkOrder>? MyWorkOrders()
    {
        if (workOrdersReadModel.Current is null && !hasFetched) _ = FetchAsync();
        return workOrdersReadModel.Current;
    }

    public IReadOnlyList<SubcontractorVariationRequest>? MyVariationRequests()
    {
        if (variationRequestsReadModel.Current is null && !hasFetched) _ = FetchAsync();
        return variationRequestsReadModel.Current;
    }

    public async Task<SubcontractorVariationRequest> RaiseVariationRequestAsync(string workOrderId, string title, string description, decimal proposedValue)
    {
        // SubcontractorId is resolved from the session server-side; the default here is ignored.
        var raised = await commands.SendAsync(
            new RaiseMyVariationRequest(workOrderId, title, description, proposedValue), CancellationToken.None);
        await variationRequestsReadModel.RefreshAsync(CancellationToken.None);
        return raised;
    }

    public async Task<WorkOrder> AcceptWorkOrderAsync(string workOrderId)
    {
        // Who accepted (and for which company) is resolved from the session server-side;
        // the command's defaulted fields are ignored there.
        var accepted = await commands.SendAsync(new AcceptMyWorkOrder(workOrderId), CancellationToken.None);
        await workOrdersReadModel.RefreshAsync(CancellationToken.None);
        return accepted;
    }

    public async Task WithdrawVariationRequestAsync(string variationRequestId)
    {
        await commands.SendAsync(new WithdrawMyVariationRequest(variationRequestId), CancellationToken.None);
        await variationRequestsReadModel.RefreshAsync(CancellationToken.None);
    }

    public Task Refresh() => FetchAsync();

    public async Task UploadDocumentAsync(string kind, DateTimeOffset? expiresAt, IBrowserFile file, CancellationToken cancellationToken,
        decimal? publicLiabilityCover = null)
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

        var response = await httpClient.PostAsync("api/portal/my/documents", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Surface the server's message (e.g. a storage error) rather than a bare status code.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        // Committed — revalidate in the background so a slow refresh can't stick the upload UI.
        _ = FetchAsync();
    }

    // Coalesces concurrent callers (e.g. Refresh() from OnInitializedAsync racing a render-time
    // MyRecord()) onto one HTTP request.
    private Task FetchAsync() => inFlight ??= FetchCoreAsync();

    private async Task FetchCoreAsync()
    {
        try
        {
            await Task.WhenAll(
                readModel.RefreshAsync(CancellationToken.None),
                workOrdersReadModel.RefreshAsync(CancellationToken.None),
                variationRequestsReadModel.RefreshAsync(CancellationToken.None));
        }
        catch { /* 401/403 (not a portal user) or transient network failure — view shows the empty state */ }
        finally
        {
            hasFetched = true;
            inFlight = null;
            OnChange?.Invoke();
        }
    }
}
