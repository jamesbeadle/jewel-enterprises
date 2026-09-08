using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;

namespace Jewel.JPMS.Api.Features.Site.Drafts;

/// <summary>
/// Opens a draft programme update from a claim: one line per live programme task, each mapped to
/// cost centres by the task's saved mapping when it has one, else by the trade-word rulebook
/// (<see cref="ProgrammeCostCentreRules"/>), and each carrying the £-weighted percentage those
/// centres have reached on the claim. Tasks neither matches are left unmapped for Claude and the
/// reviewer. Any draft already open on the project is superseded — the newest certified figures
/// are the ones worth reviewing.
///
/// <para>Adds to the context without saving, so the approval handler can run it inside its own
/// unit of work and the hand-run command inside its. Answers null when there is nothing to
/// draft — no such claim, no programme tasks, or a bill with no priced lines — which the
/// approval hook treats as "nothing to do" and the hand-run command turns into a refusal the
/// user can read.</para>
/// </summary>
internal static class ProgrammeDraftCapture
{
    public static async Task<ProgrammeDraftEntity?> OpenAsync(
        JpmsContext context, string projectId, string valuationClaimId, string openedByEmail,
        CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ValuationClaimId == valuationClaimId && row.ProjectId == projectId, cancellationToken);
        if (claim is null) return null;

        var tasks = await context.ProgrammeTasks.AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .OrderBy(task => task.PlannedStart).ThenBy(task => task.Title)
            .ToListAsync(cancellationToken);
        if (tasks.Count == 0) return null;

        var centres = await ProgrammeDraftClaimCentres.LoadAsync(context, projectId, valuationClaimId, cancellationToken);
        if (centres.Count == 0) return null;

        var savedMappings = await context.ProgrammeTaskCostCentres.AsNoTracking()
            .Where(mapping => mapping.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        var savedByTask = savedMappings
            .GroupBy(mapping => mapping.ProgrammeTaskId)
            .ToDictionary(group => group.Key, group => group.Select(mapping => mapping.CostCode).OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToList());

        await SupersedeOpenDraftsAsync(context, projectId, openedByEmail, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var draft = new ProgrammeDraftEntity
        {
            ProgrammeDraftId = SiteIdentifierFactory.NextProgrammeDraftId(),
            ProjectId = projectId,
            ValuationClaimId = valuationClaimId,
            ClaimName = claim.ToModel().DisplayName,
            Status = (int)ProgrammeDraftStatus.Open,
            CreatedAt = now,
            CreatedByEmail = openedByEmail,
            ResolvedByEmail = "",
            SuggestionsNote = ""
        };
        context.ProgrammeDrafts.Add(draft);

        var presentCodes = centres.Select(centre => centre.CostCode).ToList();
        foreach (var task in tasks)
        {
            var line = new ProgrammeDraftLineEntity
            {
                ProgrammeDraftLineId = SiteIdentifierFactory.NextProgrammeDraftLineId(),
                ProgrammeDraftId = draft.ProgrammeDraftId,
                ProgrammeTaskId = task.ProgrammeTaskId,
                TaskTitle = task.Title,
                CurrentPercent = task.ProgressPercent
            };

            if (savedByTask.TryGetValue(task.ProgrammeTaskId, out var savedCodes) && savedCodes.Count > 0)
                ProgrammeDraftLineProposal.Apply(line, centres, savedCodes, ProgrammeMappingSource.Saved);
            else
                ProgrammeDraftLineProposal.Apply(line, centres, ProgrammeCostCentreRules.Match(task.Title, presentCodes), ProgrammeMappingSource.Rule);

            context.ProgrammeDraftLines.Add(line);
        }

        return draft;
    }

    // Only the newest draft is ever Open: an older one still awaiting review is closed as
    // Superseded (never deleted — it is the record of what an earlier claim proposed).
    private static async Task SupersedeOpenDraftsAsync(
        JpmsContext context, string projectId, string byEmail, CancellationToken cancellationToken)
    {
        var open = await context.ProgrammeDrafts
            .Where(draft => draft.ProjectId == projectId && draft.Status == (int)ProgrammeDraftStatus.Open)
            .ToListAsync(cancellationToken);
        foreach (var draft in open)
        {
            draft.Status = (int)ProgrammeDraftStatus.Superseded;
            draft.ResolvedAt = DateTimeOffset.UtcNow;
            draft.ResolvedByEmail = byEmail;
        }
    }
}
