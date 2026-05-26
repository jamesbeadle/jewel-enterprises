using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public static class EffectiveRoles
{
    private static readonly IReadOnlyList<Role> AllRoles = Enum.GetValues<Role>();

    public static IReadOnlyList<Role> For(string email, DirectoryUser? directoryEntry)
    {
        if (JpmsAdministrators.Contains(email)) return AllRoles;
        if (directoryEntry is null) return Array.Empty<Role>();
        return directoryEntry.Roles;
    }
}
