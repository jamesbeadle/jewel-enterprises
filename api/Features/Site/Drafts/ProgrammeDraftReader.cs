using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Site.Drafts;

/// <summary>
/// Reads a draft as the review pane needs it: the draft, its lines in programme order (the
/// order the capture wrote them — planned start, then title), and the claim's cost centres
/// recomputed from the report so the pane can offer them to map from. The centres are
/// recomputed rather than stored because a locked claim's figures do not move, and a draft on a
/// claim that has since been reopened should show the figures as they now stand.
/// </summary>
internal static class ProgrammeDraftReader
{
    public static async Task<ProgrammeDraftDetail?> OpenForProjectAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var draft = await context.ProgrammeDrafts.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Status == (int)ProgrammeDraftStatus.Open)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return draft is null ? null : await DetailAsync(context, draft, cancellationToken);
    }

    public static async Task<ProgrammeDraftDetail> DetailAsync(
        JpmsContext context, ProgrammeDraftEntity draft, CancellationToken cancellationToken)
    {
        var lines = await context.ProgrammeDraftLines.AsNoTracking()
            .Where(line => line.ProgrammeDraftId == draft.ProgrammeDraftId)
            .ToListAsync(cancellationToken);

        // Programme order: the capture wrote the lines in planned-start order, but a row's
        // position is not stored, so re-derive it from the live tasks and fall back to title.
        var taskOrder = await context.ProgrammeTasks.AsNoTracking()
            .Where(task => task.ProjectId == draft.ProjectId)
            .Select(task => new { task.ProgrammeTaskId, task.PlannedStart })
            .ToDictionaryAsync(task => task.ProgrammeTaskId, task => task.PlannedStart, cancellationToken);

        var ordered = lines
            .OrderBy(line => taskOrder.TryGetValue(line.ProgrammeTaskId, out var start) ? start : DateTimeOffset.MaxValue)
            .ThenBy(line => line.TaskTitle, StringComparer.OrdinalIgnoreCase)
            .Select(line => line.ToModel())
            .ToList()
            .AsReadOnly();

        var centres = await ProgrammeDraftClaimCentres.LoadAsync(context, draft.ProjectId, draft.ValuationClaimId, cancellationToken);

        return new ProgrammeDraftDetail(draft.ToModel(), ordered, centres);
    }
}
