using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class CashflowSnapshotEntity
{
    [Key, MaxLength(64)] public string CashflowSnapshotId { get; set; } = "";
    public DateTimeOffset GeneratedAt { get; set; }
    public decimal ExpectedIncome13Week { get; set; }
    public decimal CommittedSpend13Week { get; set; }
    public decimal NetPosition13Week { get; set; }
}
