using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Site.Drafts;

/// <summary>
/// Writes a mapping onto a draft line and everything that follows from it — the proposed
/// percentage, the evidence trail, the source label, and whether the line takes part when the
/// draft is applied. One writer for the capture, Claude's suggestions and the reviewer's remap,
/// so a line always agrees with its own cost codes.
/// </summary>
internal static class ProgrammeDraftLineProposal
{
    public static void Apply(
        ProgrammeDraftLineEntity line,
        IReadOnlyList<ProgrammeDraftCostCentre> centres,
        IEnumerable<string> costCodes,
        ProgrammeMappingSource source)
    {
        var codes = ProgrammeDraftCostCodes.Clean(costCodes);
        line.CostCodes = ProgrammeDraftCostCodes.Join(codes);
        line.MappingSource = (int)(codes.Count == 0 ? ProgrammeMappingSource.None : source);
        line.ProposedPercent = ProgrammeProgressProposal.Propose(centres, codes);
        line.Evidence = Truncate(ProgrammeProgressProposal.Evidence(centres, codes), EvidenceMaxLength);
        // A remap starts the reviewer's decision afresh: the new proposal is the figure on offer,
        // and a line with a proposal takes part until somebody unticks it.
        line.ReviewedPercent = null;
        line.IsIncluded = line.ProposedPercent is not null;
    }

    private const int EvidenceMaxLength = 1024;

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";
}
