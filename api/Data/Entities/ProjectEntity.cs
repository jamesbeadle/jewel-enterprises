using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class ProjectEntity
{
    [Key, MaxLength(64)] public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string ClientName { get; set; } = "";
    public int Organisation { get; set; }
    public int Stage { get; set; }
    [MaxLength(256)]     public string ProjectManagerEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
