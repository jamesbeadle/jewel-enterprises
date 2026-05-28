namespace Jewel.JPMS.Api.Features.Cvr;

internal static class CvrIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextQsAccrualId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextEotId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextSnapshotId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextPackageRowId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
