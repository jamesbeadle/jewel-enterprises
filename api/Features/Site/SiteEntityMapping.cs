using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Site;

internal static class SiteEntityMapping
{
    public static SiteReport ToModel(this SiteReportEntity entity) =>
        new(entity.SiteReportId, entity.ProjectId, entity.PeriodEnd, entity.Narrative, entity.AttendanceDays, entity.OpenSnags, entity.ProgressPercent, entity.IsIssued);

    public static ProgrammeTask ToModel(this ProgrammeTaskEntity entity) =>
        new(entity.ProgrammeTaskId, entity.ProjectId, entity.Title, entity.PlannedStart, entity.PlannedEnd, entity.ProgressPercent, entity.BoqLineItemId);

    public static ProgrammeTaskLink ToModel(this ProgrammeTaskLinkEntity entity) =>
        new(entity.ProgrammeTaskLinkId, entity.ProjectId, entity.PredecessorTaskId, entity.SuccessorTaskId, entity.LagDays);

    public static ProgrammeBaseline ToModel(this ProgrammeBaselineEntity entity) =>
        new(entity.ProgrammeBaselineId, entity.ProjectId, entity.Label, entity.TakenByEmail, entity.TakenAt);

    public static ProgrammeBaselineTask ToModel(this ProgrammeBaselineTaskEntity entity) =>
        new(entity.ProgrammeBaselineTaskId, entity.ProgrammeBaselineId, entity.ProgrammeTaskId, entity.Title, entity.PlannedStart, entity.PlannedEnd);

    public static ProgrammeTaskCostCentre ToModel(this ProgrammeTaskCostCentreEntity entity) =>
        new(entity.ProgrammeTaskCostCentreId, entity.ProjectId, entity.ProgrammeTaskId, entity.CostCode);

    public static ProgrammeVariationEffect ToModel(this ProgrammeVariationEffectEntity entity) =>
        new(entity.ProgrammeVariationEffectId, entity.ProjectId, entity.VariationOrderId, entity.ProgrammeTaskId,
            entity.DelayDays, entity.Note, entity.RecordedByEmail, entity.RecordedAt);

    public static ProgrammeDraft ToModel(this ProgrammeDraftEntity entity) =>
        new(entity.ProgrammeDraftId, entity.ProjectId, entity.ValuationClaimId, entity.ClaimName,
            (ProgrammeDraftStatus)entity.Status, entity.CreatedAt, entity.CreatedByEmail,
            entity.ResolvedAt, entity.ResolvedByEmail, entity.SuggestionsRequestedAt, entity.SuggestionsNote);

    public static ProgrammeDraftLine ToModel(this ProgrammeDraftLineEntity entity) =>
        new(entity.ProgrammeDraftLineId, entity.ProgrammeDraftId, entity.ProgrammeTaskId, entity.TaskTitle,
            entity.CurrentPercent, entity.ProposedPercent, ProgrammeDraftCostCodes.Split(entity.CostCodes),
            (ProgrammeMappingSource)entity.MappingSource, entity.Evidence, entity.IsIncluded, entity.ReviewedPercent);
}

// The one place a draft line's cost codes are joined for storage and split for reading — a
// comma-separated column, because the mapping is only ever read and written whole.
internal static class ProgrammeDraftCostCodes
{
    private const char Separator = ',';

    public static string Join(IEnumerable<string> costCodes) =>
        string.Join(Separator, Clean(costCodes));

    public static IReadOnlyList<string> Split(string stored) =>
        Clean(stored.Split(Separator, StringSplitOptions.RemoveEmptyEntries)).ToList().AsReadOnly();

    // Trimmed and de-duplicated (case-insensitively — the master's own spelling is kept, since
    // "Omit's" is a real code), in the order given.
    public static IReadOnlyList<string> Clean(IEnumerable<string> costCodes) =>
        costCodes
            .Select(code => code.Trim())
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
}
