using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class SessionService
{
    private readonly AuthService auth;
    private readonly IUserDirectory directory;

    public SessionService(AuthService auth, IUserDirectory directory)
    {
        this.auth = auth;
        this.directory = directory;
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
        Adopt(resolvedUser, roles);
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

    private void Adopt(AuthenticatedUser user, IReadOnlyList<Role> roles)
    {
        CurrentUser = user;
        AvailableRoles = roles;
        ActiveRole = roles.Count == 0 ? null : MostPrivileged(roles);
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
