namespace Jewel.JPMS.Models;

public sealed record AccessRequest(
    string Email,
    string DisplayName,
    AuthProvider Provider,
    DateTimeOffset RequestedAt);
