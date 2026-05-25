using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class SessionService
{
    public AuthenticatedUser? CurrentUser { get; private set; }

    public IReadOnlyList<Role> AvailableRoles { get; private set; } = Array.Empty<Role>();

    public Role? ActiveRole { get; private set; }

    public bool IsApproved => AvailableRoles.Count > 0;

    public bool HasMultipleRoles => AvailableRoles.Count > 1;

    public event Action? OnChange;

    public void Adopt(AuthenticatedUser user, IReadOnlyList<Role> roles)
    {
        CurrentUser = user;
        AvailableRoles = roles;
        ActiveRole = roles.Count == 0 ? null : MostPrivileged(roles);
        OnChange?.Invoke();
    }

    public void SwitchTo(Role role)
    {
        if (!AvailableRoles.Contains(role)) return;
        if (ActiveRole == role) return;
        ActiveRole = role;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        CurrentUser = null;
        AvailableRoles = Array.Empty<Role>();
        ActiveRole = null;
        OnChange?.Invoke();
    }

    private static Role MostPrivileged(IReadOnlyList<Role> roles)
    {
        foreach (var role in Enum.GetValues<Role>())
        {
            if (roles.Contains(role)) return role;
        }
        return roles[0];
    }
}
