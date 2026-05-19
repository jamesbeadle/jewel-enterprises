using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Holds the currently signed-in user for the session. Today this is a mock —
/// it accepts any email the user types and records the provider they "picked".
/// When real OAuth is wired up, SignInAsync will be replaced with calls into
/// MSAL (Microsoft) and Google Identity Services and this service becomes the
/// thin layer the UI talks to.
/// </summary>
public sealed class AuthService
{
    public AuthenticatedUser? CurrentUser { get; private set; }

    public bool IsSignedIn => CurrentUser is not null;

    /// <summary>Fires whenever sign-in state changes. UI components can subscribe.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Mock sign-in. Stores the user locally and notifies subscribers.
    /// Real wiring will replace this with an OAuth code-flow callback.
    /// </summary>
    public Task SignInAsync(AuthProvider provider, string email, string? displayName = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName)
            ? DeriveNameFromEmail(email)
            : displayName!.Trim();

        CurrentUser = new AuthenticatedUser(email.Trim(), name, provider);
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        CurrentUser = null;
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    private static string DeriveNameFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "User";
        var local = email.Split('@')[0];
        var parts = local.Replace('.', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }
}
