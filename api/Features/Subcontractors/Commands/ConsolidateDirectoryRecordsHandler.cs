using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Merges duplicate directory records into one master. The winning field values (chosen side by
/// side in the consolidation dialog) are applied to the master; trades are unioned; everything
/// referencing a merged record is re-pointed to the master — work orders, bid-package invites and
/// quotes, compliance documents, workers, timesheet covers and settlement variances, variation
/// orders and requests, portal logins, company contacts and Xero links — and the merged-away
/// records are deleted. Contact details on the merged records that didn't win the master's primary
/// line are kept as company contact rows, so no email or phone number is lost. Everything happens
/// in one SaveChanges, so the merge is atomic: either the whole consolidation lands or none of it.
/// </summary>
public sealed class ConsolidateDirectoryRecordsHandler
    : ICommandHandler<ConsolidateDirectoryRecords, Subcontractor>
{
    private readonly JpmsContext context;

    public ConsolidateDirectoryRecordsHandler(JpmsContext context) { this.context = context; }

    public async Task<Subcontractor> HandleAsync(ConsolidateDirectoryRecords command, CancellationToken cancellationToken)
    {
        var mergedIds = command.MergedSubcontractorIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(id => !string.Equals(id, command.MasterSubcontractorId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (mergedIds.Count == 0)
            throw new InvalidOperationException("Nothing to consolidate — no records besides the master were given.");

        var master = await context.Subcontractors
            .FirstOrDefaultAsync(sub => sub.SubcontractorId == command.MasterSubcontractorId, cancellationToken)
            ?? throw new InvalidOperationException("The master record was not found.");

        var merged = await context.Subcontractors
            .Where(sub => mergedIds.Contains(sub.SubcontractorId))
            .ToListAsync(cancellationToken);
        if (merged.Count != mergedIds.Count)
            throw new InvalidOperationException("One or more records to consolidate were not found.");

        // Keep every distinct contact line before the winning values overwrite anything: the
        // master's own previous line and each merged record's line become company contacts unless
        // they match the chosen primary line or a contact the master already holds.
        var existingContacts = await context.CompanyContacts
            .Where(contact => contact.SubcontractorId == master.SubcontractorId
                || mergedIds.Contains(contact.SubcontractorId))
            .ToListAsync(cancellationToken);
        foreach (var record in merged.Concat(new[] { master }))
            PreserveContactLine(record, command, existingContacts);

        // The winning values, as chosen field by field in the dialog.
        master.CompanyName = command.CompanyName.Trim();
        master.ContactName = command.ContactName.Trim();
        master.ContactEmail = command.ContactEmail.Trim();
        master.ContactPhone = command.ContactPhone.Trim();
        master.CisStatus = command.CisStatus.Trim();
        master.Category = (int)command.Category;
        master.MobileNumber = command.MobileNumber.Trim();
        master.Town = command.Town.Trim();
        master.County = command.County.Trim();
        master.Website = command.Website.Trim();
        master.PaymentTermsDays = command.PaymentTermsDays;
        master.AddressLine = command.AddressLine.Trim();
        master.Postcode = command.Postcode.Trim();
        // The master keeps the earliest onboarding date — the company has been known since then.
        master.OnboardedAt = merged.Select(sub => sub.OnboardedAt).Append(master.OnboardedAt).Min();
        // Pli/PliExpiry: keep the master's unless it is blank and a merged record has one.
        if (string.IsNullOrWhiteSpace(master.Pli))
            master.Pli = merged.Select(sub => sub.Pli).FirstOrDefault(pli => !string.IsNullOrWhiteSpace(pli)) ?? master.Pli;
        if (string.IsNullOrWhiteSpace(master.PliExpiry))
            master.PliExpiry = merged.Select(sub => sub.PliExpiry).FirstOrDefault(expiry => !string.IsNullOrWhiteSpace(expiry)) ?? master.PliExpiry;

        await RepointReferencesAsync(master.SubcontractorId, mergedIds, cancellationToken);

        context.Subcontractors.RemoveRange(merged);

        await context.SaveChangesAsync(cancellationToken);

        var trades = await context.TradesBySubcontractorAsync(cancellationToken);
        var xeroLinked = await context.SubcontractorXeroLinks
            .AnyAsync(link => link.SubcontractorId == master.SubcontractorId, cancellationToken);
        return master.ToModel(
            trades.TryGetValue(master.SubcontractorId, out var tradeModels) ? tradeModels : Array.Empty<Trade>(),
            xeroLinked);
    }

    /// <summary>
    /// Keeps one record's primary contact line as a company contact on the master, unless it IS
    /// the chosen primary line or duplicates a contact already kept (matched by email when there
    /// is one, otherwise by name + phone).
    /// </summary>
    private void PreserveContactLine(SubcontractorEntity record, ConsolidateDirectoryRecords command, List<CompanyContactEntity> existingContacts)
    {
        var name = record.ContactName.Trim();
        var email = record.ContactEmail.Trim();
        var phone = record.ContactPhone.Trim();
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            return;

        var isWinningLine =
            string.Equals(email, command.ContactEmail.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(name, command.ContactName.Trim(), StringComparison.OrdinalIgnoreCase);
        if (isWinningLine) return;

        var duplicatesKept = existingContacts.Any(contact =>
            !string.IsNullOrWhiteSpace(email) && string.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(email)
                && string.Equals(contact.Name, name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(contact.Phone, phone, StringComparison.OrdinalIgnoreCase)));
        if (duplicatesKept) return;

        var kept = new CompanyContactEntity
        {
            CompanyContactId = SubcontractorIdentifierFactory.NextCompanyContactId(),
            SubcontractorId = command.MasterSubcontractorId,
            Name = name,
            Email = email,
            Phone = string.IsNullOrWhiteSpace(phone) ? record.MobileNumber.Trim() : phone,
            CreatedAt = DateTimeOffset.UtcNow
        };
        existingContacts.Add(kept);
        context.CompanyContacts.Add(kept);
    }

    /// <summary>
    /// Re-points every table that references a merged record to the master. Loads and mutates
    /// tracked entities (volumes here are directory-sized, not ledger-sized) so the whole merge
    /// commits in the handler's single SaveChanges.
    /// </summary>
    private async Task RepointReferencesAsync(string masterId, IReadOnlyList<string> mergedIds, CancellationToken ct)
    {
        // Trades: union — move each link across unless the master already holds that trade.
        var masterTradeIds = (await context.SubcontractorTrades
                .Where(link => link.SubcontractorId == masterId)
                .Select(link => link.TradeId)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mergedTradeLinks = await context.SubcontractorTrades
            .Where(link => mergedIds.Contains(link.SubcontractorId))
            .ToListAsync(ct);
        foreach (var link in mergedTradeLinks)
        {
            if (masterTradeIds.Add(link.TradeId)) link.SubcontractorId = masterId;
            else context.SubcontractorTrades.Remove(link);
        }

        // Bid-package invites: one row per (package, subcontractor), so drop a merged invite when
        // the master is already invited to the same package.
        var masterPackageIds = (await context.BidPackageRecipients
                .Where(recipient => recipient.SubcontractorId == masterId)
                .Select(recipient => recipient.BidPackageId)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mergedRecipients = await context.BidPackageRecipients
            .Where(recipient => mergedIds.Contains(recipient.SubcontractorId))
            .ToListAsync(ct);
        foreach (var recipient in mergedRecipients)
        {
            if (masterPackageIds.Add(recipient.BidPackageId)) recipient.SubcontractorId = masterId;
            else context.BidPackageRecipients.Remove(recipient);
        }

        // Quotes are real records of who quoted what — always kept, just re-pointed.
        foreach (var quote in await context.Quotes
            .Where(quote => mergedIds.Contains(quote.SubcontractorId)).ToListAsync(ct))
            quote.SubcontractorId = masterId;

        foreach (var workOrder in await context.WorkOrders
            .Where(order => mergedIds.Contains(order.SubcontractorId)).ToListAsync(ct))
            workOrder.SubcontractorId = masterId;

        foreach (var document in await context.ComplianceDocuments
            .Where(document => mergedIds.Contains(document.SubcontractorId)).ToListAsync(ct))
            document.SubcontractorId = masterId;

        foreach (var worker in await context.Workers
            .Where(worker => worker.SubcontractorId != null && mergedIds.Contains(worker.SubcontractorId)).ToListAsync(ct))
            worker.SubcontractorId = masterId;

        foreach (var cover in await context.XeroLineTimesheetCovers
            .Where(cover => mergedIds.Contains(cover.SubcontractorId)).ToListAsync(ct))
            cover.SubcontractorId = masterId;

        foreach (var variance in await context.LabourSettlementVariances
            .Where(variance => mergedIds.Contains(variance.SubcontractorId)).ToListAsync(ct))
            variance.SubcontractorId = masterId;

        foreach (var order in await context.VariationOrders
            .Where(order => order.SelectedSubcontractorId != null && mergedIds.Contains(order.SelectedSubcontractorId)).ToListAsync(ct))
            order.SelectedSubcontractorId = masterId;

        foreach (var request in await context.SubcontractorVariationRequests
            .Where(request => mergedIds.Contains(request.SubcontractorId)).ToListAsync(ct))
            request.SubcontractorId = masterId;

        // Portal logins linked to a merged record follow it — their sessions then scope to the
        // master's data instead of a record that no longer exists.
        foreach (var user in await context.DirectoryUsers
            .Where(user => user.SubcontractorId != null && mergedIds.Contains(user.SubcontractorId)).ToListAsync(ct))
            user.SubcontractorId = masterId;

        foreach (var contact in await context.CompanyContacts
            .Where(contact => mergedIds.Contains(contact.SubcontractorId)).ToListAsync(ct))
            contact.SubcontractorId = masterId;

        // Xero links move across — this is what keeps a master built from any Xero-imported
        // record marked "linked to Xero" after the merge.
        foreach (var link in await context.SubcontractorXeroLinks
            .Where(link => mergedIds.Contains(link.SubcontractorId)).ToListAsync(ct))
            link.SubcontractorId = masterId;
    }
}
