using System.Reflection;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

/// <summary>
/// Thrown when the delete cannot proceed for a reason the dialog should show next to the confirm
/// field (the typed name doesn't match). The endpoint maps it to 409, which HttpCommandSender
/// surfaces as the CommandFailedException message without raising the error toast.
/// </summary>
public sealed class DeleteProjectRefusedException : Exception
{
    public DeleteProjectRefusedException(string message) : base(message) { }
}

/// <summary>
/// Permanently deletes a project and everything filed under it, in one transaction:
///
/// 1. Child rows that carry no ProjectId of their own (request items/messages, drawing revisions,
///    quote lines, work-order lines, ...) are deleted by joining through their parent while the
///    parent rows still exist. JPMS declares no FK relationships (records link by loose string
///    id — see JpmsContext), so nothing cascades and nothing enforces an order; children go first
///    purely so the parent-id subqueries still have rows to select.
/// 2. Every entity in the model that carries a ProjectId is then swept generically, so a future
///    project-scoped table is covered the day it is added rather than silently orphaned. Three
///    are excluded: the project row itself (deleted last), AuditEvents (the append-only trail is
///    the record that things happened — it survives, and gains a ProjectDeleted event), and
///    XeroLedgerLines (Xero's facts, not ours — their allocation is cleared instead, returning
///    the lines to the unallocated queue; this project's XeroCostSplits rows DO carry a ProjectId
///    and are swept).
///
/// Blob-stored content (drawing files, attachments) is not touched — only the database rows that
/// reference it. The mailbox is likewise untouched: filed emails keep their tags in Outlook.
/// LeadOutcomes.CreatedProjectId (a column not named ProjectId, so outside the sweep) is left
/// dangling deliberately: the lead's conversion history is its own record, like the audit trail.
/// </summary>
public sealed class DeleteProjectHandler : ICommandHandler<DeleteProject, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public DeleteProjectHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteProject command, CancellationToken cancellationToken)
    {
        var project = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);

        // Already gone: the deletion this command asks for has happened. Acknowledge rather than
        // fail, so a retry after a timeout doesn't read as an error.
        if (project is null) return new Acknowledgement(command.ProjectId);

        if (!string.Equals(project.Name.Trim(), command.ConfirmName.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DeleteProjectRefusedException("The name typed doesn't match this project's name — nothing was deleted.");

        var projectId = command.ProjectId;
        var reference = project.Reference;
        var name = project.Name;

        // EnableRetryOnFailure is on (Program.cs), so the explicit transaction must run inside the
        // execution strategy: on a transient failure the WHOLE deletion retries as one unit.
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            // --- 1. Children keyed by a parent id rather than ProjectId, grandchildren first. ---

            var requestIds = context.Requests.Where(r => r.ProjectId == projectId).Select(r => r.RequestId);
            await context.RequestItems.Where(x => requestIds.Contains(x.RequestId)).ExecuteDeleteAsync(cancellationToken);
            await context.RequestMessages.Where(x => requestIds.Contains(x.RequestId)).ExecuteDeleteAsync(cancellationToken);

            var drawingIds = context.Drawings.Where(d => d.ProjectId == projectId).Select(d => d.DrawingId);
            var revisionIds = context.DrawingRevisions.Where(r => drawingIds.Contains(r.DrawingId)).Select(r => r.DrawingRevisionId);
            await context.DrawingIssueRecords.Where(x => revisionIds.Contains(x.DrawingRevisionId)).ExecuteDeleteAsync(cancellationToken);
            await context.DrawingRevisions.Where(x => drawingIds.Contains(x.DrawingId)).ExecuteDeleteAsync(cancellationToken);
            await context.DrawingFolders.Where(x => x.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);

            var bidPackageIds = context.BidPackages.Where(p => p.ProjectId == projectId).Select(p => p.BidPackageId);
            var quoteIds = context.Quotes.Where(q => bidPackageIds.Contains(q.BidPackageId)).Select(q => q.QuoteId);
            await context.QuoteLineItems.Where(x => quoteIds.Contains(x.QuoteId)).ExecuteDeleteAsync(cancellationToken);
            await context.Quotes.Where(x => bidPackageIds.Contains(x.BidPackageId)).ExecuteDeleteAsync(cancellationToken);
            await context.BidPackageRecipients.Where(x => bidPackageIds.Contains(x.BidPackageId)).ExecuteDeleteAsync(cancellationToken);
            await context.BidPackageLineItems.Where(x => bidPackageIds.Contains(x.BidPackageId)).ExecuteDeleteAsync(cancellationToken);
            await context.BidPackageDrawings.Where(x => bidPackageIds.Contains(x.BidPackageId)).ExecuteDeleteAsync(cancellationToken);

            var workOrderIds = context.WorkOrders.Where(w => w.ProjectId == projectId).Select(w => w.WorkOrderId);
            await context.WorkOrderLines.Where(x => workOrderIds.Contains(x.WorkOrderId)).ExecuteDeleteAsync(cancellationToken);

            var hsRecordIds = context.HsRecords.Where(h => h.ProjectId == projectId).Select(h => h.HsRecordId);
            await context.HsRecordAttendance.Where(x => hsRecordIds.Contains(x.HsRecordId)).ExecuteDeleteAsync(cancellationToken);

            var baselineIds = context.ProgrammeBaselines.Where(b => b.ProjectId == projectId).Select(b => b.ProgrammeBaselineId);
            await context.ProgrammeBaselineTasks.Where(x => baselineIds.Contains(x.ProgrammeBaselineId)).ExecuteDeleteAsync(cancellationToken);

            var programmeDraftIds = context.ProgrammeDrafts.Where(d => d.ProjectId == projectId).Select(d => d.ProgrammeDraftId);
            await context.ProgrammeDraftLines.Where(x => programmeDraftIds.Contains(x.ProgrammeDraftId)).ExecuteDeleteAsync(cancellationToken);

            var progressReportIds = context.ProgressReports.Where(r => r.ProjectId == projectId).Select(r => r.ProgressReportId);
            await context.ProgressReportSelections.Where(x => progressReportIds.Contains(x.ProgressReportId)).ExecuteDeleteAsync(cancellationToken);

            var claimIds = context.ValuationClaims.Where(c => c.ProjectId == projectId).Select(c => c.ValuationClaimId);
            await context.ClaimLines.Where(x => claimIds.Contains(x.ValuationClaimId)).ExecuteDeleteAsync(cancellationToken);

            var prelimItemIds = context.PrelimItems.Where(p => p.ProjectId == projectId).Select(p => p.PrelimItemId);
            await context.PrelimForecastEntries.Where(x => prelimItemIds.Contains(x.PrelimItemId)).ExecuteDeleteAsync(cancellationToken);

            var invoiceIds = context.ValuationInvoices.Where(i => i.ProjectId == projectId).Select(i => i.ValuationInvoiceId);
            await context.ValuationInvoiceEvents.Where(x => invoiceIds.Contains(x.ValuationInvoiceId)).ExecuteDeleteAsync(cancellationToken);

            var snapshotIds = context.ValuationReportSnapshots.Where(s => s.ProjectId == projectId).Select(s => s.ValuationReportSnapshotId);
            await context.ValuationReportSnapshotLines.Where(x => snapshotIds.Contains(x.ValuationReportSnapshotId)).ExecuteDeleteAsync(cancellationToken);

            var instructionIds = context.ArchitectInstructions.Where(i => i.ProjectId == projectId).Select(i => i.ArchitectInstructionId);
            await context.ArchitectInstructionVariations.Where(x => instructionIds.Contains(x.ArchitectInstructionId)).ExecuteDeleteAsync(cancellationToken);

            // --- 2. Xero ledger lines: clear the allocation, keep the Xero facts. ---
            //
            // Two shapes of allocation point at a project: the line itself (line.ProjectId), or —
            // for a line whose value is shared — rows in XeroCostSplits, in which case the line's
            // own ProjectId may be null. Either way the WHOLE line drops back to Unallocated and
            // ALL its split rows go (a partial split can no longer sum to the line's Net, which
            // XeroCostSplits guarantees), so the other projects' shares return to the queue for
            // re-allocation rather than being left silently inconsistent.

            await context.XeroLedgerLines
                .Where(line => line.ProjectId == projectId ||
                               context.XeroCostSplits.Any(split =>
                                   split.XeroLedgerLineId == line.XeroLedgerLineId && split.ProjectId == projectId))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(line => line.AllocationStatus, 0) // Unallocated
                    .SetProperty(line => line.ProjectId, (string?)null)
                    .SetProperty(line => line.CostCenterCode, (string?)null)
                    .SetProperty(line => line.AllocatedBy, (string?)null)
                    .SetProperty(line => line.AllocatedAtUtc, (DateTimeOffset?)null), cancellationToken);

            await context.XeroCostSplits
                .Where(split => context.XeroCostSplits.Any(other =>
                    other.XeroLedgerLineId == split.XeroLedgerLineId && other.ProjectId == projectId))
                .ExecuteDeleteAsync(cancellationToken);

            // --- 3. Generic sweep: every entity carrying a ProjectId, current and future. ---

            foreach (var entityType in context.Model.GetEntityTypes())
            {
                if (entityType.FindProperty(nameof(ProjectEntity.ProjectId)) is null) continue;
                var clrType = entityType.ClrType;
                if (clrType == typeof(ProjectEntity)) continue;        // deleted last, below
                if (clrType == typeof(AuditEventEntity)) continue;     // the trail survives
                if (clrType == typeof(XeroLedgerLineEntity)) continue; // unallocated above

                var delete = (Task<int>)DeleteRowsMethod.MakeGenericMethod(clrType)
                    .Invoke(null, new object[] { context, projectId, cancellationToken })!;
                await delete;
            }

            // --- 4. The project row itself. ---
            await context.Projects.Where(p => p.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        // Best-effort by design (see AuditTrail): written after the deletion has succeeded. The
        // project's rows are gone, so the event itself is the surviving record of what and who —
        // hence CancellationToken.None: a client disconnecting mid-request must not cancel it.
        await audit.WriteAsync(
            AuditEventType.ProjectDeleted,
            $"Project \"{name}\" and all its records were permanently deleted.",
            projectId: projectId,
            recordReference: reference,
            cancellationToken: CancellationToken.None);

        return new Acknowledgement(projectId);
    }

    private static readonly MethodInfo DeleteRowsMethod = typeof(DeleteProjectHandler)
        .GetMethod(nameof(DeleteRowsForProjectAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static Task<int> DeleteRowsForProjectAsync<TEntity>(
        JpmsContext context, string projectId, CancellationToken cancellationToken) where TEntity : class =>
        context.Set<TEntity>()
            .Where(row => EF.Property<string>(row, nameof(ProjectEntity.ProjectId)) == projectId)
            .ExecuteDeleteAsync(cancellationToken);
}
