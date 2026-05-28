using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public static class InitialRoleSelection
{
    public static Role? From(IReadOnlyList<Role> availableRoles, Role? persistedRole)
    {
        if (availableRoles.Count == 0) return null;
        if (persistedRole is not null && availableRoles.Contains(persistedRole.Value)) return persistedRole;
        return MostPrivileged(availableRoles);
    }

    private static Role MostPrivileged(IReadOnlyList<Role> availableRoles)
    {
        foreach (var role in Enum.GetValues<Role>())
        {
            if (availableRoles.Contains(role)) return role;
        }
        return availableRoles[0];
    }
}
