using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Site.Drafts;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

/// <summary>
/// Asks Claude about the draft's still-unmatched tasks — the ones neither a saved mapping nor
/// the rulebook could place — and writes what it suggests onto those lines as Claude-sourced
/// mappings, proposals recomputed. One call for all of them, under the client's 35s ceiling.
///
/// <para>Follows the one-shot convention: unconfigured, failed or unparsable degrades to a
/// sentence on the draft (SuggestionsNote) and never to an error, and SuggestionsRequestedAt is
/// stamped whatever happened so the review pane asks once, not on every open.</para>
/// </summary>
public sealed class SuggestProgrammeDraftMappingsHandler : ICommandHandler<SuggestProgrammeDraftMappings, ProgrammeDraftDetail>
{
    private const int MaxSampleDescriptionsPerCentre = 4;
    private const int MaxSampleDescriptionChars = 60;
    private const int ResponseTokenBudget = 4000;

    private readonly JpmsContext context;
    private readonly IClaudeClient claude;
    private readonly ILogger<SuggestProgrammeDraftMappingsHandler> logger;

    public SuggestProgrammeDraftMappingsHandler(JpmsContext context, IClaudeClient claude, ILogger<SuggestProgrammeDraftMappingsHandler> logger)
    {
        this.context = context; this.claude = claude; this.logger = logger;
    }

    public async Task<ProgrammeDraftDetail> HandleAsync(SuggestProgrammeDraftMappings command, CancellationToken cancellationToken)
    {
        var draft = await context.ProgrammeDrafts
            .FirstOrDefaultAsync(row => row.ProgrammeDraftId == command.ProgrammeDraftId, cancellationToken)
            ?? throw new InvalidOperationException("That draft programme update no longer exists.");
        if (draft.Status != (int)ProgrammeDraftStatus.Open)
            throw new InvalidOperationException("That draft has already been closed.");

        var lines = await context.ProgrammeDraftLines
            .Where(line => line.ProgrammeDraftId == draft.ProgrammeDraftId)
            .ToListAsync(cancellationToken);
        var unmatched = lines.Where(line => string.IsNullOrWhiteSpace(line.CostCodes)).ToList();

        draft.SuggestionsRequestedAt = DateTimeOffset.UtcNow;
        draft.SuggestionsNote = await SuggestAsync(draft, unmatched, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return await ProgrammeDraftReader.DetailAsync(context, draft, cancellationToken);
    }

    private async Task<string> SuggestAsync(
        ProgrammeDraftEntity draft, List<ProgrammeDraftLineEntity> unmatched, CancellationToken cancellationToken)
    {
        if (unmatched.Count == 0)
            return "Every task already had a cost centre — there was nothing to ask Claude.";
        if (!claude.IsConfigured)
            return "The AI isn't connected (no Anthropic key is configured), so the remaining tasks need mapping by hand.";

        var centres = await ProgrammeDraftClaimCentres.LoadAsync(context, draft.ProjectId, draft.ValuationClaimId, cancellationToken);
        if (centres.Count == 0)
            return "The valuation report has no priced lines to map to.";

        var samples = await SampleDescriptionsByCodeAsync(draft.ProjectId, cancellationToken);
        var tasks = unmatched.Select(line => (line.ProgrammeTaskId, Title: line.TaskTitle)).ToList();

        var response = await claude.CompleteAsync(
            ProgrammeDraftSuggestionPrompt.System,
            ProgrammeDraftSuggestionPrompt.User(tasks, centres, samples),
            cancellationToken,
            maxTokensOverride: ResponseTokenBudget);

        var presentCodes = centres.Select(centre => centre.CostCode).ToList();
        var suggestions = ProgrammeDraftSuggestionPrompt.Parse(response, tasks.Select(task => task.ProgrammeTaskId).ToList(), presentCodes);
        if (suggestions.Count == 0)
        {
            logger.LogWarning("Programme draft {ProgrammeDraftId}: Claude's mapping answer was empty or unusable.", draft.ProgrammeDraftId);
            return "Claude didn't give a usable answer, so the remaining tasks need mapping by hand.";
        }

        var mapped = 0;
        foreach (var suggestion in suggestions.Where(suggestion => suggestion.CostCodes.Count > 0))
        {
            var line = unmatched.FirstOrDefault(candidate => candidate.ProgrammeTaskId == suggestion.ProgrammeTaskId);
            if (line is null) continue;
            ProgrammeDraftLineProposal.Apply(line, centres, suggestion.CostCodes, ProgrammeMappingSource.Claude);
            if (!string.IsNullOrWhiteSpace(suggestion.Why))
                line.Evidence = WithReason(line.Evidence, suggestion.Why);
            mapped++;
        }

        var left = unmatched.Count - mapped;
        return left == 0
            ? $"Claude suggested cost centres for all {mapped} remaining task{(mapped == 1 ? "" : "s")} — check them before applying."
            : $"Claude suggested cost centres for {mapped} of {unmatched.Count} remaining tasks; {left} still need{(left == 1 ? "s" : "")} mapping by hand.";
    }

    // The largest lines under each cost centre, as a few words each — what lets the model tell
    // "Hot and cold water supply" from "Above ground drainage" behind one plumbing code.
    private async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> SampleDescriptionsByCodeAsync(
        string projectId, CancellationToken cancellationToken)
    {
        var lines = await context.ValuationLineItems.AsNoTracking()
            .Where(line => line.ProjectId == projectId && line.LineAmount > 0m)
            .Select(line => new { line.CostCode, line.Description, line.LineAmount })
            .ToListAsync(cancellationToken);

        return lines
            .GroupBy(line => line.CostCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .OrderByDescending(line => line.LineAmount)
                    .Select(line => Shorten(line.Description))
                    .Where(description => description.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaxSampleDescriptionsPerCentre)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string Shorten(string description)
    {
        var trimmed = description.Trim();
        return trimmed.Length <= MaxSampleDescriptionChars ? trimmed : trimmed[..MaxSampleDescriptionChars].TrimEnd() + "…";
    }

    private const int EvidenceMaxLength = 1024;

    private static string WithReason(string evidence, string why)
    {
        var combined = string.IsNullOrWhiteSpace(evidence) ? $"Claude: {why}" : $"{evidence} — Claude: {why}";
        return combined.Length <= EvidenceMaxLength ? combined : combined[..(EvidenceMaxLength - 1)] + "…";
    }
}
