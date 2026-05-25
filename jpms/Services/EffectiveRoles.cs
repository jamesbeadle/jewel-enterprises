using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public static class EffectiveRoles
{
    public static IReadOnlyList<Role> For(string email, DirectoryUser? directoryEntry)
    {
        var roles = new List<Role>();

        if (directoryEntry is not null)
        {
            roles.AddRange(directoryEntry.Roles);
        }

        if (JpmsAdministrators.Contains(email) && !roles.Contains(Role.Admin))
        {
            roles.Insert(0, Role.Admin);
        }

        return roles.AsReadOnly();
    }
}
