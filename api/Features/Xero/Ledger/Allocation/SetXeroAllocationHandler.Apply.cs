using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class SetXeroAllocationHandler
{
    /// <summary>What each action leaves on the line. A discussion message leaves it untouched —
    /// the message row is added by the thread step.</summary>
    private static void ApplyToLine(Batch batch, XeroLedgerLineEntity line)
    {
        switch (batch.Action)
        {
            case XeroAllocationAction.Allocate: Allocate(batch, line); break;
            case XeroAllocationAction.AllocateToBucket: Stamp(batch, line, XeroAllocationStatus.Bucketed, null, null, batch.Command.Bucket); break;
            case XeroAllocationAction.Ignore: Stamp(batch, line, XeroAllocationStatus.Ignored, null, null, null); break;
            case XeroAllocationAction.Reset: Reset(line); break;
            case XeroAllocationAction.SetProject: SetProject(batch.Command, line); break;
            case XeroAllocationAction.Dispute: Dispute(batch, line); break;
            case XeroAllocationAction.ResolveDispute: ResolveDispute(line); break;
        }
    }

    /// <summary>A split line carries its projects + centres in XeroCostSplits (reconciled
    /// already), never on the line itself.</summary>
    private static void Allocate(Batch batch, XeroLedgerLineEntity line) =>
        Stamp(batch, line, XeroAllocationStatus.Allocated,
            batch.Splits is null ? batch.SingleProject : batch.CommonSplitProject,
            batch.Splits is null ? batch.SingleCode : null,
            null);

    private static void Stamp(Batch batch, XeroLedgerLineEntity line, XeroAllocationStatus status, string? projectId, string? costCenterCode, string? bucket)
    {
        line.AllocationStatus = (int)status;
        line.ProjectId = projectId;
        line.CostCenterCode = costCenterCode;
        line.Bucket = bucket;
        line.AllocatedBy = batch.Command.AllocatedBy;
        line.AllocatedAtUtc = batch.Now;
        line.Note = batch.Command.Note;
    }

    private static void Reset(XeroLedgerLineEntity line)
    {
        line.AllocationStatus = (int)XeroAllocationStatus.Unallocated;
        line.ProjectId = null;
        line.CostCenterCode = null;
        line.Bucket = null;
        line.AllocatedBy = null;
        line.AllocatedAtUtc = null;
        line.Note = null;
    }

    /// <summary>The half-step: project decided (or, with null, un-decided). The line keeps its
    /// status — Unallocated (queued under its project) or Disputed — and is not stamped as
    /// allocated; Allocate does that. A supplied cost centre is saved too (the dispute-stage
    /// agreement); unsetting the project clears both.</summary>
    private static void SetProject(SetXeroAllocation command, XeroLedgerLineEntity line)
    {
        line.ProjectId = command.ProjectId;
        line.CostCenterCode = command.ProjectId is null ? null
            : command.CostCenterCode ?? line.CostCenterCode;
    }

    /// <summary>Parked for the conversation: the line leaves the queue (or its allocation) and
    /// sits in the Disputed bucket. Coding already on the line is KEPT as the position under
    /// discussion; AllocatedBy/AtUtc become the "disputed by / on" stamp and Note the opening
    /// message (also first row of the thread).</summary>
    private static void Dispute(Batch batch, XeroLedgerLineEntity line)
    {
        line.AllocationStatus = (int)XeroAllocationStatus.Disputed;
        line.Bucket = null;
        line.AllocatedBy = batch.Command.AllocatedBy;
        line.AllocatedAtUtc = batch.Now;
        line.Note = batch.Command.Note;
    }

    /// <summary>Either side returns the line to the queue, agreed coding kept: with a project
    /// (and centre) saved it lands on that project's tab armed for Allocate. The thread
    /// survives as history.</summary>
    private static void ResolveDispute(XeroLedgerLineEntity line)
    {
        line.AllocationStatus = (int)XeroAllocationStatus.Unallocated;
        line.AllocatedBy = null;
        line.AllocatedAtUtc = null;
        line.Note = null;
    }
}
