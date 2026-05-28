using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class DirectoryUserEntity
{
    [Key, MaxLength(256)] public string Email { get; set; } = "";
    [MaxLength(256)]      public string DisplayName { get; set; } = "";
}

public sealed class DirectoryUserRoleEntity
{
    [Key, MaxLength(64)] public string DirectoryUserRoleId { get; set; } = "";
    [MaxLength(256)]     public string DirectoryUserEmail { get; set; } = "";
    public int Role { get; set; }
}

public sealed class AccessRequestEntity
{
    [Key, MaxLength(256)] public string Email { get; set; } = "";
    [MaxLength(256)]      public string DisplayName { get; set; } = "";
    public int Provider { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
}
