using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class SessionService
{
    private readonly AuthService auth;
    private readonly IUserDirectory directory;
    private readonly ActiveRoleStorage roleStorage;

    public SessionService(AuthService auth, IUserDirectory directory, ActiveRoleStorage roleStorage)
    {
        this.auth = auth;
        this.directory = directory;
        this.roleStorage = roleStorage;
    }

    public AuthenticatedUser? CurrentUser { get; private set; }

    public IReadOnlyList<Role> AvailableRoles { get; private set; } = Array.Empty<Role>();

    public Role? ActiveRole { get; private set; }

    public bool IsApproved => AvailableRoles.Count > 0;

    public bool HasMultipleRoles => AvailableRoles.Count > 1;

    public event Action? OnChange;

    public async Task EnsureLoadedAsync()
    {
        if (CurrentUser is not null) return;

        await auth.EnsureInitialisedAsync();
        if (!auth.IsSignedIn) return;

        var signedInUser = auth.CurrentUser!;
        var directoryEntry = directory.Find(signedInUser.Email);
        var roles = EffectiveRoles.For(signedInUser.Email, directoryEntry);
        var displayName = string.IsNullOrWhiteSpace(directoryEntry?.DisplayName)
            ? signedInUser.DisplayName
            : directoryEntry!.DisplayName;
        var resolvedUser = signedInUser with { DisplayName = displayName };
        var persistedRole = await roleStorage.ReadAsync(resolvedUser.Email);
        Adopt(resolvedUser, roles, persistedRole);
    }

    public void SwitchTo(Role role)
    {
        if (!AvailableRoles.Contains(role)) return;
        if (ActiveRole == role) return;
        ActiveRole = role;
        _ = PersistActiveRole();
        OnChange?.Invoke();
    }

    public void Clear()
    {
        CurrentUser = null;
        AvailableRoles = Array.Empty<Role>();
        ActiveRole = null;
        OnChange?.Invoke();
    }

    private void Adopt(AuthenticatedUser user, IReadOnlyList<Role> roles, Role? persistedRole)
    {
        CurrentUser = user;
        AvailableRoles = roles;
        ActiveRole = ChooseInitialRole(roles, persistedRole);
        OnChange?.Invoke();
    }

    private static Role? ChooseInitialRole(IReadOnlyList<Role> roles, Role? persistedRole)
    {
        if (roles.Count == 0) return null;
        if (persistedRole is not null && roles.Contains(persistedRole.Value)) return persistedRole;
        return MostPrivileged(roles);
    }

    private async Task PersistActiveRole()
    {
        if (CurrentUser is null || ActiveRole is null) return;
        await roleStorage.WriteAsync(CurrentUser.Email, ActiveRole.Value);
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
