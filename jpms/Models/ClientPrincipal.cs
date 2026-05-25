namespace Jewel.JPMS.Models;

public sealed record ClientPrincipal(
    string IdentityProvider,
    string UserId,
    string UserDetails,
    IReadOnlyList<string> UserRoles);

public sealed record ClientPrincipalEnvelope(ClientPrincipal? ClientPrincipal);
