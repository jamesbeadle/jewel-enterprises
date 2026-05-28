using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Cashflow;

internal static class CashflowEntityMapping
{
    public static CashflowSnapshot ToModel(this CashflowSnapshotEntity entity) => new(
        CashflowSnapshotId: entity.CashflowSnapshotId,
        GeneratedAt: entity.GeneratedAt,
        ExpectedIncome13Week: entity.ExpectedIncome13Week,
        CommittedSpend13Week: entity.CommittedSpend13Week,
        NetPosition13Week: entity.NetPosition13Week);
}
