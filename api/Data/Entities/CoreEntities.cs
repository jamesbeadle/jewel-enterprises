using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class LeadEntity
{
    [Key, MaxLength(64)] public string LeadId { get; set; } = "";
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(256)]     public string ContactName { get; set; } = "";
    [MaxLength(256)]     public string ContactEmail { get; set; } = "";
    [MaxLength(64)]      public string ContactPhone { get; set; } = "";
    [MaxLength(256)]     public string CompanyName { get; set; } = "";
    [MaxLength(512)]     public string SiteAddress { get; set; } = "";
    public decimal? EstimatedValue { get; set; }
    public int Source { get; set; }
    public int Stage { get; set; }
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";
    public DateTimeOffset CapturedAt { get; set; }
}

public sealed class BoqLineItemEntity
{
    [Key, MaxLength(64)] public string BoqLineItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(32)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal RateValue { get; set; }
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    public int Discipline { get; set; }
}

public sealed class RateEntity
{
    [Key, MaxLength(64)] public string RateId { get; set; } = "";
    [MaxLength(64)]      public string Trade { get; set; } = "";
    [MaxLength(256)]     public string Description { get; set; } = "";
    [MaxLength(16)]      public string Unit { get; set; } = "";
    public decimal Value { get; set; }
    [MaxLength(256)]     public string SupplierName { get; set; } = "";
    public DateTimeOffset LastPricedAt { get; set; }
}

public sealed class DrawingEntity
{
    [Key, MaxLength(64)] public string DrawingId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string DrawingCode { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(16)]      public string CurrentRevision { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DrawingRevisionEntity
{
    [Key, MaxLength(64)] public string DrawingRevisionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingId { get; set; } = "";
    [MaxLength(16)]      public string RevisionLabel { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    [MaxLength(256)]     public string IssuedByEmail { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? SupersededAt { get; set; }
    public bool IsAmbiguous { get; set; }
    public int ViewCount { get; set; }
}
