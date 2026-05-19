namespace Jewel.JPMS.Models;

/// <summary>
/// Identity provider a user signed in with.
/// </summary>
public enum AuthProvider
{
    Microsoft,
    Google
}

/// <summary>
/// A signed-in user. In the mock implementation we only carry an email + the provider
/// they used. Once real OAuth is wired up, this will also carry tenant id, object id,
/// access token expiry, etc.
/// </summary>
public sealed record AuthenticatedUser(
    string Email,
    string DisplayName,
    AuthProvider Provider
);

/// <summary>
/// Internal record of a user the company admin has approved for the platform.
/// Source of truth (today) is the hard-coded list in <see cref="Services.AllowListUserDirectory"/>.
/// </summary>
public sealed record DirectoryUser(
    string Email,
    string DisplayName,
    string Role
);
