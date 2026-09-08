using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Site;

// ---- Draft programme updates (decision 2026-09-08) ---------------------------------------------
// The certified valuation's percentages, proposed onto the programme's tasks for review. See
// docs/Programme-Draft-From-Valuation-Spec.md for the flow; ProgrammeDraft (Models) for the shape.

/// <summary>The project's open draft with its lines and the claim's cost centres, or null when
/// no draft is awaiting review.</summary>
public sealed record GetOpenProgrammeDraft(string ProjectId) : IQuery<ProgrammeDraftDetail?>;

/// <summary>Opens a draft programme update from a locked claim (Preapproved or Confirmed — a
/// Draft claim's percentages are still moving). Saved mappings and the rulebook fill what they
/// can; any draft already open on the project is superseded. The approval handler runs the same
/// capture automatically; this is the Programme tab's hand-run of it.</summary>
public sealed record DraftProgrammeFromValuation(
    string ProjectId,
    string ValuationClaimId,
    string RequestedByEmail) : ICommand<ProgrammeDraftDetail>;

/// <summary>Asks Claude to map the draft's still-unmatched tasks to the claim's cost centres.
/// Degrades to a note on the draft when the AI is unconfigured or answers nothing usable —
/// never an error. Stamps SuggestionsRequestedAt either way.</summary>
public sealed record SuggestProgrammeDraftMappings(string ProgrammeDraftId) : ICommand<ProgrammeDraftDetail>;

/// <summary>The reviewer's decision on one line: the cost centres it maps to (a change
/// recomputes the proposal and marks the mapping Person), whether applying should touch the
/// task, and their own percentage when the proposal is not the figure they want (null keeps
/// the proposal).</summary>
public sealed record ReviewProgrammeDraftLine(
    string ProgrammeDraftLineId,
    IReadOnlyList<string> CostCodes,
    bool IsIncluded,
    decimal? ReviewedPercent) : ICommand<ProgrammeDraftLine>;

/// <summary>Writes every included line's percentage onto its task, saves the confirmed
/// mappings on the tasks, and closes the draft as Applied.</summary>
public sealed record ApplyProgrammeDraft(string ProgrammeDraftId, string AppliedByEmail) : ICommand<ProgrammeDraft>;

/// <summary>Closes the draft as Discarded; the programme is untouched.</summary>
public sealed record DiscardProgrammeDraft(string ProgrammeDraftId, string DiscardedByEmail) : ICommand<ProgrammeDraft>;
