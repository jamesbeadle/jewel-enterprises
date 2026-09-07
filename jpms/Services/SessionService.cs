
namespace Jewel.JPMS.Services;

public sealed class SessionService : IDisposable
{
    /// <summary>How long a "Viewing as" switch away from the user's own role lasts before the app
    /// defaults back, for users opted in on Admin → Users (DirectoryUser.RevertToOwnRole).</summary>
    public static readonly TimeSpan RevertAfter = TimeSpan.FromHours(2);

    /// <summary>How often the open tab checks whether the two-hour window has run out. A closed
    /// tab needs no watch — Adopt applies the same rule to the persisted role on next load.</summary>
    private static readonly TimeSpan RevertWatchInterval = TimeSpan.FromSeconds(30);

    private readonly AuthService auth;
    private readonly ActiveRoleStorage roleStorage;

    private Timer? revertWatch;
    private bool revertDueRaised;

    public SessionService(AuthService auth, ActiveRoleStorage roleStorage)
    {
        this.auth = auth;
        this.roleStorage = roleStorage;
    }

    public AuthenticatedUser? CurrentUser { get; private set; }

    public IReadOnlyList<Role> AvailableRoles { get; private set; } = Array.Empty<Role>();

    public Role? ActiveRole { get; private set; }

    /// <summary>The user's own role — first directory-assigned role that isn't Administrator,
    /// resolved server-side (HomeRoleSelection). Null when unknown (single-role users don't need
    /// it; older API builds don't send it).</summary>
    public Role? HomeRole { get; private set; }

    public bool IsApproved => AvailableRoles.Count > 0;

    public bool HasMultipleRoles => AvailableRoles.Count > 1;

    /// <summary>True for users opted in to the two-hour default-back (Admin → Users), once their
    /// own role is known. Everyone else keeps the original behaviour: the switcher sticks
    /// indefinitely.</summary>
    public bool RevertsToHomeRole => auth.CurrentRevertToOwnRole && HomeRole is not null && HasMultipleRoles;

    /// <summary>When the user switched away from their own role — the start of the two-hour
    /// window. Null while on their own role, or when reverting is off for them.</summary>
    public DateTimeOffset? SwitchedAwayAt { get; private set; }

    /// <summary>When the current temporary role defaults back to <see cref="HomeRole"/>.</summary>
    public DateTimeOffset? RevertDueAt => SwitchedAwayAt + RevertAfter;

    /// <summary>True while an opted-in user is viewing as a role that isn't their own.</summary>
    public bool IsTemporaryRole => RevertsToHomeRole && ActiveRole is not null && ActiveRole != HomeRole;

    public event Action? OnChange;

    /// <summary>The two-hour window has run out. RoleOverridePrompt listens and asks — and if the
    /// prompt goes unanswered, reverts quietly. Raised once per window; ExtendTemporaryRole
    /// re-arms it.</summary>
    public event Action? OnRevertDue;

    /// <summary>True once the session has been resolved for a signed-in user. Read synchronously
    /// by the page gate so an in-app navigation, where the answer is already known, never flashes
    /// a loading state for a question nobody is waiting on.</summary>
    public bool IsLoaded => CurrentUser is not null;

    public async Task EnsureLoadedAsync()
    {
        if (CurrentUser is not null) return;

        await auth.EnsureInitialisedAsync();
        if (!auth.IsSignedIn) return;

        // Roles and display name are resolved server-side and returned by /api/auth/me.
        var signedInUser = auth.CurrentUser!;
        var roles = auth.CurrentRoles;
        var persistedRole = await roleStorage.ReadAsync(signedInUser.Email);
        Adopt(signedInUser, roles, persistedRole);
    }

    public void SwitchTo(Role role)
    {
        if (!AvailableRoles.Contains(role)) return;
        if (ActiveRole == role) return;
        ActiveRole = role;
        SwitchedAwayAt = IsTemporaryRole ? DateTimeOffset.UtcNow : null;
        revertDueRaised = false;
        _ = PersistActiveRole();
        UpdateRevertWatch();
        OnChange?.Invoke();
    }

    /// <summary>Keeps the current temporary role for another two hours — the prompt's "carry on"
    /// answer. Restarts the window from now, in storage too, so a reload keeps the extension.</summary>
    public void ExtendTemporaryRole()
    {
        if (!IsTemporaryRole) return;
        SwitchedAwayAt = DateTimeOffset.UtcNow;
        revertDueRaised = false;
        _ = PersistActiveRole();
        UpdateRevertWatch();
        OnChange?.Invoke();
    }

    /// <summary>Back to the user's own role — the prompt's other answer, and what happens when
    /// the prompt is ignored.</summary>
    public void RevertToHomeRole()
    {
        if (HomeRole is { } home && ActiveRole != home) SwitchTo(home);
    }

    public void Clear()
    {
        CurrentUser = null;
        AvailableRoles = Array.Empty<Role>();
        ActiveRole = null;
        HomeRole = null;
        SwitchedAwayAt = null;
        revertDueRaised = false;
        UpdateRevertWatch();
        OnChange?.Invoke();
    }

    private void Adopt(AuthenticatedUser user, IReadOnlyList<Role> roles, ActiveRoleStorage.StoredRole? persisted)
    {
        CurrentUser = user;
        AvailableRoles = roles;
        HomeRole = auth.CurrentHomeRole is { } home && roles.Contains(home) ? home : null;

        if (RevertsToHomeRole)
        {
            // Opted in: a persisted switch survives only while its two-hour window is still open.
            // Anything older — including roles stored before the timestamp existed, which is
            // every "stuck on Administrator since yesterday" case — defaults back to their own
            // role. Their own role is also the starting point, never MostPrivileged.
            var own = HomeRole!.Value;
            if (persisted is { } stored && stored.Role != own && roles.Contains(stored.Role)
                && stored.SwitchedAt is { } at && DateTimeOffset.UtcNow - at < RevertAfter)
            {
                ActiveRole = stored.Role;
                SwitchedAwayAt = at;
            }
            else
            {
                ActiveRole = own;
                SwitchedAwayAt = null;
                // Write the revert down, so every other tab sharing the storage agrees.
                if (persisted?.Role != own) _ = PersistActiveRole();
            }
        }
        else
        {
            ActiveRole = InitialRoleSelection.From(roles, persisted?.Role);
            SwitchedAwayAt = null;
        }
        revertDueRaised = false;
        UpdateRevertWatch();
        OnChange?.Invoke();
    }

    private async Task PersistActiveRole()
    {
        if (CurrentUser is null || ActiveRole is null) return;
        await roleStorage.WriteAsync(CurrentUser.Email, ActiveRole.Value, SwitchedAwayAt);
    }

    private void UpdateRevertWatch()
    {
        if (IsTemporaryRole && !revertDueRaised)
        {
            revertWatch ??= new Timer(_ => CheckRevertDue(), null, RevertWatchInterval, RevertWatchInterval);
        }
        else
        {
            revertWatch?.Dispose();
            revertWatch = null;
        }
    }

    private void CheckRevertDue()
    {
        if (revertDueRaised || !IsTemporaryRole) return;
        if (RevertDueAt is { } due && DateTimeOffset.UtcNow >= due)
        {
            revertDueRaised = true;
            OnRevertDue?.Invoke();
        }
    }

    public void Dispose() => revertWatch?.Dispose();
}
