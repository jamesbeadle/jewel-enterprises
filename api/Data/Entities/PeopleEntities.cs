using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class SubcontractorEntity
{
    [Key, MaxLength(64)] public string SubcontractorId { get; set; } = "";
    [MaxLength(256)]     public string CompanyName { get; set; } = "";
    [MaxLength(64)]      public string PrimaryTrade { get; set; } = "";
    [MaxLength(256)]     public string ContactName { get; set; } = "";
    [MaxLength(256)]     public string ContactEmail { get; set; } = "";
    [MaxLength(64)]      public string ContactPhone { get; set; } = "";
    [MaxLength(32)]      public string CisStatus { get; set; } = "";
    public DateTimeOffset OnboardedAt { get; set; }
}

public sealed class ComplianceDocumentEntity
{
    [Key, MaxLength(64)] public string ComplianceDocumentId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    [MaxLength(128)]     public string Kind { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

public sealed class HsRecordEntity
{
    [Key, MaxLength(64)] public string HsRecordId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(512)]     public string Summary { get; set; } = "";
    public int Severity { get; set; }
    public int Status { get; set; }
    [MaxLength(256)]     public string AssignedToEmail { get; set; } = "";
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
