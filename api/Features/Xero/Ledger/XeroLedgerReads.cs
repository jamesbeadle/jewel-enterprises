using Microsoft.EntityFrameworkCore;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// The pieces every ledger read shares: the cost splits for a set of lines, the suggester, and
/// the entity → model projection. Pulled out so the whole-ledger read, the per-status read and the
/// per-project read can't drift apart in how they shape a line.
/// </summary>
internal static partial class XeroLedgerReads
{
    /// <summary>
    /// The cost splits belonging to the lines being returned, keyed by line id.
    ///
    /// This used to read the WHOLE XeroCostSplits table and group it in memory on every request,
    /// however few lines were actually being shown. Splits only ever exist on allocated lines, so
    /// an unallocated page now issues no split query at all.
    /// </summary>
    public static async Task<Dictionary<string, IReadOnlyList<XeroCostSplit>>> SplitsForAsync(
        JpmsContext context, IReadOnlyList<XeroLedgerLineEntity> entities, CancellationToken cancellationToken)
    {
        var ids = entities
            .Where(entity => entity.AllocationStatus == (int)XeroAllocationStatus.Allocated)
            .Select(entity => entity.XeroLedgerLineId)
            .Distinct()
            .ToList();
        if (ids.Count == 0) return new Dictionary<string, IReadOnlyList<XeroCostSplit>>();

        var rows = await context.XeroCostSplits.AsNoTracking()
            .Where(split => ids.Contains(split.XeroLedgerLineId))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(split => split.XeroLedgerLineId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<XeroCostSplit>)group
                .Select(split => new XeroCostSplit(split.CostCenterCode, split.Net, split.ProjectId))
                .ToList());
    }

    /// <summary>
    /// The discussion threads for the disputed lines being returned, keyed by line id, oldest
    /// first. Only disputed lines carry their thread — the Disputed tab is the only place it
    /// renders, and it is small — so every other status issues no message query at all.
    /// </summary>
    public static async Task<Dictionary<string, IReadOnlyList<XeroDisputeMessage>>> DisputeMessagesForAsync(
        JpmsContext context, IReadOnlyList<XeroLedgerLineEntity> entities, CancellationToken cancellationToken)
    {
        var ids = entities
            .Where(entity => entity.AllocationStatus == (int)XeroAllocationStatus.Disputed)
            .Select(entity => entity.XeroLedgerLineId)
            .Distinct()
            .ToList();
        if (ids.Count == 0) return new Dictionary<string, IReadOnlyList<XeroDisputeMessage>>();

        var rows = await context.XeroDisputeMessages.AsNoTracking()
            .Where(message => ids.Contains(message.XeroLedgerLineId))
            .OrderBy(message => message.SentAtUtc)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(message => message.XeroLedgerLineId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<XeroDisputeMessage>)group
                .Select(message => new XeroDisputeMessage(message.Author, message.Body, message.SentAtUtc))
                .ToList());
    }

    /// <summary>
    /// The suggester, built only when some line actually needs a suggestion. Building it reads the
    /// project and cost-centre tables, so an Allocated / Bucketed / Ignored page skips both.
    /// </summary>
    public static async Task<XeroAllocationSuggester?> SuggesterForAsync(
        JpmsContext context, IReadOnlyList<XeroLedgerLineEntity> entities, CancellationToken cancellationToken)
    {
        if (!entities.Any(entity => entity.AllocationStatus == (int)XeroAllocationStatus.Unallocated))
            return null;

        var projects = await context.Projects.AsNoTracking().ToListAsync(cancellationToken);
        var costCenters = await context.CostCenters.AsNoTracking()
            .Where(centre => centre.IsActive)
            .ToListAsync(cancellationToken);
        return new XeroAllocationSuggester(projects, costCenters);
    }

    public static XeroLedgerLine ToModel(
        XeroLedgerLineEntity entity, IReadOnlyList<XeroCostSplit>? splits, XeroAllocationSuggester? suggester,
        IReadOnlyList<XeroDisputeMessage>? disputeMessages = null,
        LabourSupplierRecognition.LineRecognition? labour = null,
        WorkOrderBillRecognition.LineVerdict? workOrderBill = null,
        WorkOrderBillApprovalStamp? workOrderApproval = null)
    {
        // Suggestions only matter while a line still needs a decision.
        var unallocated = entity.AllocationStatus == (int)XeroAllocationStatus.Unallocated;
        return new XeroLedgerLine(
            entity.XeroLedgerLineId,
            entity.XeroInvoiceId,
            entity.Type,
            entity.InvoiceNumber,
            entity.Reference,
            entity.ContactName,
            entity.Date,
            entity.InvoiceStatus,
            entity.Description,
            entity.Net,
            entity.Tax,
            entity.AccountCode,
            entity.AccountName,
            entity.XeroSite,
            entity.XeroCostCode,
            (XeroAllocationStatus)entity.AllocationStatus,
            entity.ProjectId,
            entity.CostCenterCode,
            entity.Bucket,
            entity.AllocatedBy,
            entity.AllocatedAtUtc,
            entity.Note,
            unallocated ? suggester?.SuggestProject(entity.XeroSite) : null,
            unallocated ? suggester?.SuggestCostCenter(entity.XeroCostCode) : null,
            unallocated ? suggester?.SuggestBucket(entity.ContactName, entity.Description) : null,
            entity.FirstSeenAtUtc,
            entity.LastSyncedAtUtc,
            splits,
            (XeroWriteBackStatus)entity.WriteBackStatus,
            entity.WriteBackError,
            entity.WriteBackAtUtc,
            entity.HasAttachments,
            disputeMessages,
            labour?.MatchedWorkerId,
            labour?.MatchedWorkerName,
            labour?.MatchedSubcontractorId,
            labour?.CoveredByTimesheets ?? false,
            labour?.CoveredPeriodStart,
            workOrderBill?.Match,
            workOrderBill?.ExceptionReason,
            workOrderApproval);
    }
}
