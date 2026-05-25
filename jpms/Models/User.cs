namespace Jewel.JPMS.Models;

public enum AuthProvider
{
    Microsoft,
    Google
}

public sealed record AuthenticatedUser(
    string Email,
    string DisplayName,
    AuthProvider Provider);

public sealed record DirectoryUser(
    string Email,
    string DisplayName,
    Role Role);
