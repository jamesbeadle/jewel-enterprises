using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class UpdateRequestDetailsHandler : ICommandHandler<UpdateRequestDetails, Request>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly ILogger<UpdateRequestDetailsHandler> logger;
    public UpdateRequestDetailsHandler(JpmsContext context, IMailboxGraphClient graph, ILogger<UpdateRequestDetailsHandler> logger)
    {
        this.context = context;
        this.graph = graph;
        this.logger = logger;
    }

    public async Task<Request> HandleAsync(UpdateRequestDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        // Allow the reference to be edited manually, but never onto a number another request on this
        // project already holds — excluding this request so an unchanged reference always passes.
        await RequestReferenceGuard.EnsureUniqueAsync(context, entity.ProjectId, command.Reference, entity.RequestId, cancellationToken);

        // The mailbox tag is derived from the (project-qualified) reference, so capture the old tag
        // before we change it. The project qualifier is the same on both sides of the rename.
        var projectRef = await RequestTags.ProjectRefAsync(context, entity.ProjectId, cancellationToken);
        var previousTag = TriageCategories.ForRecord(RequestTags.Stem(projectRef, entity.ProjectId, entity.TagReference));

        entity.Reference = command.Reference;
        entity.Title = command.Title;
        entity.Description = command.Description;
        entity.Status = (int)command.Status;
        entity.Value = command.Value;
        entity.ResponseText = command.ResponseText;
        entity.RespondedByEmail = command.RespondedByEmail;
        entity.ImpliesVariation = command.ImpliesVariation;
        entity.DrawingRef = command.DrawingRef;
        entity.ResponseDue = command.ResponseDue;
        entity.RelatedDrawingSpec = command.RelatedDrawingSpec;
        entity.InternalNotes = command.InternalNotes;
        entity.ClientNotes = command.ClientNotes;
        // EOT -> NoD provenance only makes sense on an EOT; never write it for other kinds. A null on
        // the command means "not supplied" (the parameter is optional and most edit surfaces don't
        // carry it), NOT "clear the link" — so an existing link is preserved unless a value arrives.
        if ((RequestType)entity.Kind == RequestType.ExtensionOfTime && command.RelatedNodRequestId is not null)
            entity.RelatedNodRequestId = string.IsNullOrWhiteSpace(command.RelatedNodRequestId) ? null : command.RelatedNodRequestId;
        ApplyExtensionDays(entity, command);
        if (command.RaisedAt is { } raised) entity.RaisedAt = raised;
        // The issue date is user-managed: a supplied value (over)writes it; null means "not
        // supplied" (most edit surfaces don't carry it), so an existing date is preserved.
        if (command.IssuedAt is { } issued) entity.IssuedAt = issued;
        // The Critical Path tag follows the same convention: a supplied value (over)writes it,
        // null means "not supplied" — so surfaces that don't carry the tag never shed it.
        if (command.CriticalPath is { } criticalPath) entity.CriticalPath = criticalPath;
        // The nudge dismissal follows suit: only the banner's "No" ever supplies a value, so no
        // other edit surface can resurrect (or re-dismiss) the two-week prompt.
        if (command.CriticalPathNudgeDismissed is { } nudgeDismissed) entity.CriticalPathNudgeDismissed = nudgeDismissed;
        if (entity.RespondedAt is null && !string.IsNullOrWhiteSpace(command.ResponseText)) entity.RespondedAt = DateTimeOffset.UtcNow;

        // The close date lives and dies with the Closed status. While closed it is user-manageable
        // (today or earlier); a null on the command means "not supplied", not "clear" — most edit
        // surfaces don't carry it. Moving to Closed without a date stamps now; leaving Closed
        // (reopening) clears the date so a later close records its own.
        if (command.Status == RequestStatus.Closed)
            entity.ClosedAt = command.ClosedAt ?? entity.ClosedAt ?? DateTimeOffset.UtcNow;
        else
            entity.ClosedAt = null;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (RequestReferenceConflict.IsReferenceClash(ex))
        {
            // The guard above is check-then-act and can race; the unique index is the backstop.
            throw RequestReferenceConflict.AsFriendlyError(command.Reference);
        }

        // If the reference changed, its mailbox tag changed with it. Move every email carrying the old
        // JPMS/<ref> tag onto the new one so the record keeps its linked correspondence — including
        // replies further down the tagged threads. Best-effort: the reference is already saved, and a
        // transient Graph failure shouldn't fail the edit (a later thread-sync can reconcile).
        var newTag = TriageCategories.ForRecord(RequestTags.Stem(projectRef, entity.ProjectId, entity.TagReference));
        if (!string.Equals(previousTag, newTag, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var moved = await graph.RetagAsync(previousTag, newTag, cancellationToken);
                logger.LogInformation("Retagged {Count} email(s) from {OldTag} to {NewTag} after reference change.",
                    moved, previousTag, newTag);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Retag from {OldTag} to {NewTag} failed; emails may still carry the old tag.",
                    previousTag, newTag);
            }
        }

        // Return with the itemised queries so the detail view keeps them across an edit round-trip.
        var items = await context.RequestItems
            .Where(item => item.RequestId == entity.RequestId)
            .ToListAsync(cancellationToken);
        return entity.ToModel(items);
    }

    // The days sought / awarded only mean anything on an EOT, and follow the "null means not
    // supplied" convention: only the surfaces that carry them (the Claims form, the Programme
    // tab's EOT row) ever write them.
    private static void ApplyExtensionDays(RequestEntity entity, UpdateRequestDetails command)
    {
        if ((RequestType)entity.Kind != RequestType.ExtensionOfTime) return;
        if (command.EotDaysClaimed is { } claimed) entity.EotDaysClaimed = claimed;
        if (command.EotDaysGranted is { } granted) entity.EotDaysGranted = granted;
    }
}
