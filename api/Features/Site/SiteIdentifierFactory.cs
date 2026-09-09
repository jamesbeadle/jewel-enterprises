namespace Jewel.JPMS.Api.Features.Site;

internal static class SiteIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextSiteReportId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgrammeTaskId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgrammeTaskLinkId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgrammeBaselineId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgrammeBaselineTaskId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgrammeTaskCostCentreId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgrammeDraftId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgrammeDraftLineId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgrammeVariationEffectId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
