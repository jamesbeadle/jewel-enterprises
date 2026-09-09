using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RaiseRequestHandler : ICommandHandler<RaiseRequest, Request>
{
    // Auto-minted references are retried on a unique-index clash (a concurrent create took the same
    // number); manual ones are not — the user picked that number, so they get the clash error instead.
    private const int MaxMintAttempts = 3;

    private readonly JpmsContext context;
    public RaiseRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<Request> HandleAsync(RaiseRequest command, CancellationToken cancellationToken)
    {
        // The reference may have been typed manually, so guard against re-using a number already
        // on this project before we insert. (Fast path — the unique index is the real backstop.)
        await RequestReferenceGuard.EnsureUniqueAsync(context, command.ProjectId, command.Reference, excludeRequestId: null, cancellationToken);

        var manualReference = !string.IsNullOrWhiteSpace(command.Reference);

        RequestEntity entity;
        for (var attempt = 1; ; attempt++)
        {
            var nextNumber = (await context.Requests.MaxAsync(r => (int?)r.Number, cancellationToken) ?? 0) + 1;

            // A blank reference is minted server-side by kind: a General request is a container
            // numbered REQ-#### to match its display number (global sequence — the number doubles as
            // its mailbox folder name in the shared projects@ mailbox); any other kind continues the
            // project's own sequence (e.g. "RFI-048" -> "RFI-049"). A typed reference (e.g. a
            // back-filled legacy RFI) is honoured as given.
            var reference = manualReference
                ? command.Reference.Trim()
                : await MintReferenceAsync(command.ProjectId, command.Kind, nextNumber, cancellationToken);

            entity = new RequestEntity
            {
                RequestId = RequestsIdentifierFactory.Next(),
                Number = nextNumber,
                ProjectId = command.ProjectId,
                Kind = (int)command.Kind,
                Reference = reference,
                Title = command.Title,
                Description = command.Description,
                // No explicit status means a brand-new record, which starts where the raise policy
                // says: an official instrument is already with the correspondent (Open), a General
                // container is ours to act on (Needs action). Backfills pass their status through.
                Status = (int)(command.Status ?? command.Kind.DefaultStatusOnRaise()),
                Value = command.Value,
                RaisedByEmail = command.RaisedByEmail,
                RaisedAt = command.RaisedAt ?? DateTimeOffset.UtcNow,
                // "Date issued" is the one date shown across the register — stamp it on creation
                // (today, or the backfill date for a historical record). RaisedAt survives only as
                // the internal created-on audit stamp; the user edits IssuedAt thereafter.
                IssuedAt = command.RaisedAt ?? DateTimeOffset.UtcNow,
                RespondedAt = command.RespondedAt,
                ResponseText = command.ResponseText,
                RespondedByEmail = command.RespondedByEmail,
                // A backfilled record created already-Closed takes its response date as the close
                // date (the best evidence of when it actually closed), else the backfill moment.
                ClosedAt = (command.Status ?? command.Kind.DefaultStatusOnRaise()) == RequestStatus.Closed
                    ? command.RespondedAt ?? DateTimeOffset.UtcNow
                    : null,
                ImpliesVariation = false,
                DrawingRef = command.DrawingRef,
                ResponseDue = command.ResponseDue,
                RelatedDrawingSpec = null,
                InternalNotes = command.InternalNotes,
                ClientNotes = command.ClientNotes,
                // EOT -> NoD provenance only makes sense on an EOT; ignore it for every other kind.
                RelatedNodRequestId = command.Kind == RequestType.ExtensionOfTime ? command.RelatedNodRequestId : null,
                EotDaysClaimed = command.Kind == RequestType.ExtensionOfTime ? command.EotDaysClaimed : null,
                EotDaysGranted = command.Kind == RequestType.ExtensionOfTime ? command.EotDaysGranted : null
            };
            context.Requests.Add(entity);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException ex) when (RequestReferenceConflict.IsReferenceClash(ex))
            {
                // Detach the rejected insert so the retry starts clean.
                context.Entry(entity).State = EntityState.Detached;
                if (manualReference || attempt >= MaxMintAttempts)
                    throw RequestReferenceConflict.AsFriendlyError(reference);
            }
        }

        // No email is drafted here — creating a request (even an emailable kind: RFI / NOD / EOT)
        // is a pure register action. A draft is only created when a person explicitly asks for one
        // (PrepareRequestEmailDraft / PrepareRequestReplyDraft).
        return entity.ToModel();
    }

    private async Task<string> MintReferenceAsync(string projectId, RequestType kind, int nextNumber, CancellationToken cancellationToken)
    {
        if (kind == RequestType.General) return $"REQ-{nextNumber:0000}";

        var projectReferences = await context.Requests
            .Where(r => r.ProjectId == projectId)
            .Select(r => r.Reference)
            .ToListAsync(cancellationToken);
        return RequestReference.SuggestNext(kind, projectReferences);
    }
}
