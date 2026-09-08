using Jewel.JPMS.Api.Features.Site.Drafts;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Queries;

// The project's draft programme update awaiting review, with its lines and the claim's cost
// centres — or null, which is the usual answer: most of the time nothing is awaiting review,
// and the client's Nothing<T>() turns the 204 into a null the Programme tab reads as "no draft".
public sealed class GetOpenProgrammeDraftHandler : IQueryHandler<GetOpenProgrammeDraft, ProgrammeDraftDetail?>
{
    private readonly JpmsContext context;
    public GetOpenProgrammeDraftHandler(JpmsContext context) { this.context = context; }

    public Task<ProgrammeDraftDetail?> HandleAsync(GetOpenProgrammeDraft query, CancellationToken cancellationToken) =>
        ProgrammeDraftReader.OpenForProjectAsync(context, query.ProjectId, cancellationToken);
}
