using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class SetXeroAllocationHandler
{
    /// <summary>The refusals, action by action, before anything is touched.</summary>
    private async Task GuardAsync(Batch batch, CancellationToken cancellationToken)
    {
        var lines = batch.Lines;
        switch (batch.Action)
        {
            case XeroAllocationAction.Allocate:
                await ValidateAllocateAsync(batch, cancellationToken);
                break;
            case XeroAllocationAction.AllocateToBucket:
                if (!XeroBuckets.All.Contains(batch.Command.Bucket ?? "", StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Choose a bucket (Parking, Fuel, Software subscriptions or Other).");
                break;
            case XeroAllocationAction.SetProject:
                await GuardSetProjectAsync(batch, cancellationToken);
                break;
            case XeroAllocationAction.Dispute:
                if (lines.Any(line => line.AllocationStatus != (int)XeroAllocationStatus.Unallocated
                                      && line.AllocationStatus != (int)XeroAllocationStatus.Allocated))
                    throw new InvalidOperationException("Dispute applies to queued or allocated lines.");
                break;
            case XeroAllocationAction.AddDisputeMessage:
                if (string.IsNullOrWhiteSpace(batch.Command.Note))
                    throw new InvalidOperationException("Write a message.");
                if (lines.Any(line => line.AllocationStatus != (int)XeroAllocationStatus.Disputed))
                    throw new InvalidOperationException("Messages can only be added to disputed lines.");
                break;
            case XeroAllocationAction.ResolveDispute:
                if (lines.Any(line => line.AllocationStatus != (int)XeroAllocationStatus.Disputed))
                    throw new InvalidOperationException("Only disputed lines can be returned to allocation.");
                break;
        }
    }

    /// <summary>
    /// A null project is a deliberate unset (clears the saved coding and the Xero site); a
    /// supplied project must exist. Disputed lines take Set too (2026-08-14): the coding the
    /// two sides are converging on is saved mid-dispute where both can see it, ready for the
    /// moment the line is resolved back into the queue.
    /// </summary>
    private async Task GuardSetProjectAsync(Batch batch, CancellationToken cancellationToken)
    {
        var command = batch.Command;
        if (command.ProjectId is not null
            && !await context.Projects.AnyAsync(project => project.ProjectId == command.ProjectId, cancellationToken))
            throw new InvalidOperationException("Choose a project to set.");
        var queuedOrDisputed = new[] { (int)XeroAllocationStatus.Unallocated, (int)XeroAllocationStatus.Disputed };
        if (batch.Lines.Any(line => !queuedOrDisputed.Contains(line.AllocationStatus)))
            throw new InvalidOperationException("Set applies to queued or disputed lines only.");
        if (command.CostCenterCode is not null
            && !await context.CostCenters.AnyAsync(
                centre => centre.Code == command.CostCenterCode && centre.IsActive, cancellationToken))
            throw new InvalidOperationException("Choose an active cost centre.");
    }
}
