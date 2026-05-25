using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class BidPackageEntity
{
    [Key, MaxLength(64)] public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(64)]      public string Trade { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";
}

public sealed class QuoteEntity
{
    [Key, MaxLength(64)] public string QuoteId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    public decimal Value { get; set; }
    [MaxLength(1024)]    public string Notes { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
    public bool IsDeclined { get; set; }
}

public sealed class WorkOrderEntity
{
    [Key, MaxLength(64)] public string WorkOrderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    public decimal Value { get; set; }
    [MaxLength(1024)]    public string Scope { get; set; } = "";
    public DateTimeOffset AwardedAt { get; set; }
    [MaxLength(256)]     public string AwardedByEmail { get; set; } = "";
}

public sealed class ChangeRecordEntity
{
    [Key, MaxLength(64)] public string ChangeRecordId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Description { get; set; } = "";
    public int Status { get; set; }
    public decimal? Value { get; set; }
    [MaxLength(256)]     public string RaisedByEmail { get; set; } = "";
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}
