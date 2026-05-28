namespace Jewel.JPMS.Api.Features.Cashflow;

internal static class CashflowIdentifierFactory
{
    public static string NextSnapshotId() => Guid.NewGuid().ToString("N");
}
