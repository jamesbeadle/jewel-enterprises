namespace Jewel.JPMS.Models;

// A draft programme update: the programme's tasks with the progress the certified valuation
// says each has reached, held for a person to review before anything on the live programme
// changes (decision 2026-09-08). One is opened automatically when the architect's approval is
// recorded on a valuation invoice — the certification moment — and can be opened by hand from
// the Programme tab against any locked claim. Only the newest draft on a project is Open: opening
// another supersedes it. Applying writes the accepted percentages onto the tasks and saves the
// confirmed cost-centre mappings; discarding changes nothing.
public enum ProgrammeDraftStatus
{
    Open = 0,
    Applied = 1,
    Discarded = 2,
    Superseded = 3
}

// Where a draft line's cost-centre mapping came from — shown on the line so the reviewer knows
// how much to trust the proposal: a mapping a person confirmed on an earlier draft (Saved), a
// trade word in the task's title (Rule), Claude reading the title against the bill (Claude), or
// this draft's reviewer (Person). None means nothing matched and the task needs a hand.
public enum ProgrammeMappingSource
{
    None = 0,
    Saved = 1,
    Rule = 2,
    Claude = 3,
    Person = 4
}

public static class ProgrammeDraftStatusExtensions
{
    public static string DisplayName(this ProgrammeDraftStatus status) => status switch
    {
        ProgrammeDraftStatus.Open => "Awaiting review",
        ProgrammeDraftStatus.Applied => "Applied",
        ProgrammeDraftStatus.Discarded => "Discarded",
        ProgrammeDraftStatus.Superseded => "Superseded",
        _ => status.ToString()
    };
}

public static class ProgrammeMappingSourceExtensions
{
    public static string DisplayName(this ProgrammeMappingSource source) => source switch
    {
        ProgrammeMappingSource.None => "Unmatched",
        ProgrammeMappingSource.Saved => "Saved mapping",
        ProgrammeMappingSource.Rule => "Matched by rule",
        ProgrammeMappingSource.Claude => "Suggested by Claude",
        ProgrammeMappingSource.Person => "Set by reviewer",
        _ => source.ToString()
    };
}

public sealed record ProgrammeDraft(
    string ProgrammeDraftId,
    string ProjectId,
    string ValuationClaimId,
    string ClaimName,                       // the claim's DisplayName at the time, so the draft reads on its own
    ProgrammeDraftStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedByEmail,                  // the approver (automatic) or the person who asked for it
    DateTimeOffset? ResolvedAt,             // applied / discarded / superseded
    string ResolvedByEmail,
    // Claude has been asked about the unmatched tasks (or there was nothing to ask): the review
    // pane asks once, automatically, and never again unless a person presses the button.
    DateTimeOffset? SuggestionsRequestedAt,
    string SuggestionsNote)                 // what that ask came back with, in a sentence
{
    public bool IsOpen => Status == ProgrammeDraftStatus.Open;
}

// One programme task on the draft: what it is at today, what the valuation proposes, and the
// reviewer's decision. CostCodes is the mapping the proposal was computed from; Evidence says
// which lines and how much (£ claimed of £ amount) so the number can be checked against the
// report. IsIncluded false leaves the task untouched when the draft is applied.
public sealed record ProgrammeDraftLine(
    string ProgrammeDraftLineId,
    string ProgrammeDraftId,
    string ProgrammeTaskId,
    string TaskTitle,                       // copied, so the draft still reads if the task is renamed
    decimal CurrentPercent,                 // the task's progress when the draft was opened
    decimal? ProposedPercent,               // null when no cost centre is mapped or none has priced lines
    IReadOnlyList<string> CostCodes,
    ProgrammeMappingSource MappingSource,
    string Evidence,
    bool IsIncluded,
    decimal? ReviewedPercent)               // the reviewer's own figure, when they overrode the proposal
{
    public bool IsMapped => CostCodes.Count > 0;

    // What applying writes: the reviewer's figure when there is one, else the proposal.
    public decimal? PercentToApply => ReviewedPercent ?? ProposedPercent;

    public bool ChangesTheTask => IsIncluded && PercentToApply is { } percent && percent != CurrentPercent;
}

// One cost centre as it stands on the draft's claim: the priced lines behind it summed, so the
// review pane can show "MEC-PLM Plumber · 96.5%" beside every task and offer the list to map from.
public sealed record ProgrammeDraftCostCentre(
    string CostCode,
    string Name,
    int LineCount,
    decimal Amount,                         // Σ line amounts (positive, priced lines only)
    decimal Claimed)                        // Σ cumulative claimed on the claim
{
    public decimal Percent => ProgrammeProgressProposal.Percent(Claimed, Amount);
}

// The review pane's payload: the draft, its lines, and the claim's cost centres.
public sealed record ProgrammeDraftDetail(
    ProgrammeDraft Draft,
    IReadOnlyList<ProgrammeDraftLine> Lines,
    IReadOnlyList<ProgrammeDraftCostCentre> CostCentres)
{
    public int MappedCount => Lines.Count(line => line.IsMapped);
    public int UnmappedCount => Lines.Count - MappedCount;
    public int ChangeCount => Lines.Count(line => line.ChangesTheTask);
}
