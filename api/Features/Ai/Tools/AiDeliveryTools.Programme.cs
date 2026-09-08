using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool GetProgramme()
    {
        return new(
            "get_programme",
            "The Programme tab in one answer: the live programme (tasks with planned dates and "
            + "percent complete, finish-to-start dependency links with lag) with its baselines — "
            + "immutable snapshots of the whole programme, newest first, the latest being the "
            + "yardstick slippage is measured against — plus the project's Liquidated Damages "
            + "claims (the client's claims for late completion: LAD refs, delay period, days, "
            + "rate, amount, status). A claim's LAD reference is its mailbox tag stem, so tagged "
            + "emails link to it as evidence. Also each task's confirmed cost-centre mapping and, "
            + "when one is awaiting review, the draft programme update opened from the certified "
            + "valuation (which task the valuation says has reached what, and what a person still "
            + "has to map) — reviewed and applied on the Programme tab.",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
            AiToolKind.Read,
            ProgrammeReaders,
            GetProgrammeAsync);
    }

    private static async Task<string> GetProgrammeAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);

        var detail = await Query<GetProgrammeDetail, ProgrammeDetail>(context, new GetProgrammeDetail(projectId), ct);
        var claims = await Query<ListLadClaimsForProject, IReadOnlyList<LadClaim>>(
            context, new ListLadClaimsForProject(projectId), ct);
        var draft = await Query<GetOpenProgrammeDraft, ProgrammeDraftDetail?>(context, new GetOpenProgrammeDraft(projectId), ct);
        return Serialise(new
        {
            ok = true,
            projectId,
            tasks = detail.Tasks,
            links = detail.Links,
            latestBaseline = detail.Baseline,
            latestBaselineTasks = detail.BaselineTasks,
            baselines = detail.Baselines,
            taskCostCentres = detail.CostCentres,
            draftProgrammeUpdate = draft is null ? null : DraftRow(draft),
            ladClaims = claims.Select(ClaimRow)
        });
    }

    private static object DraftRow(ProgrammeDraftDetail draft) => new
    {
        draft.Draft.ProgrammeDraftId,
        draft.Draft.ClaimName,
        status = draft.Draft.Status.DisplayName(),
        draft.Draft.CreatedAt,
        draft.Draft.CreatedByEmail,
        draft.Draft.SuggestionsNote,
        mapped = draft.MappedCount,
        unmapped = draft.UnmappedCount,
        changes = draft.ChangeCount,
        lines = draft.Lines.Select(line => new
        {
            line.ProgrammeTaskId,
            line.TaskTitle,
            line.CurrentPercent,
            line.ProposedPercent,
            line.ReviewedPercent,
            line.IsIncluded,
            line.CostCodes,
            mappingSource = line.MappingSource.DisplayName(),
            line.Evidence
        }),
        route = $"/projects/{draft.Draft.ProjectId}/programme"
    };

    private static object ClaimRow(LadClaim claim) => new
    {
        claim.LadClaimId,
        claim.Reference,
        claim.Title,
        claim.Description,
        claim.PeriodFrom,
        claim.PeriodTo,
        claim.DaysClaimed,
        claim.RatePerWeek,
        claim.Amount,
        status = claim.Status.ToString(),
        claim.RaisedAt
    };
}
