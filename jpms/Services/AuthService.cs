using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class AuthService
{
    private const string AuthMeEndpoint = "/.auth/me";
    private const string AzureActiveDirectoryProviderId = "aad";

    private readonly HttpClient httpClient;
    private bool isInitialised;

    public AuthService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public AuthenticatedUser? CurrentUser { get; private set; }

    public bool IsSignedIn => CurrentUser is not null;

    public event Action? OnChange;

    public async Task EnsureInitialisedAsync()
    {
        if (isInitialised) return;
        await LoadCurrentUserAsync();
        isInitialised = true;
    }

    public async Task RefreshAsync()
    {
        await LoadCurrentUserAsync();
        OnChange?.Invoke();
    }

    private async Task LoadCurrentUserAsync()
    {
        var envelope = await TryFetchPrincipalAsync();
        CurrentUser = envelope?.ClientPrincipal is null
            ? null
            : BuildAuthenticatedUser(envelope.ClientPrincipal);
    }

    private async Task<ClientPrincipalEnvelope?> TryFetchPrincipalAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ClientPrincipalEnvelope>(AuthMeEndpoint);
        }
        catch
        {
            return null;
        }
    }

    private static AuthenticatedUser BuildAuthenticatedUser(ClientPrincipal principal)
    {
        var provider = principal.IdentityProvider == AzureActiveDirectoryProviderId
            ? AuthProvider.Microsoft
            : AuthProvider.Google;

        var email = principal.UserDetails;
        var displayName = DisplayNameFromEmail(email);
        return new AuthenticatedUser(email, displayName, provider);
    }

    private static string DisplayNameFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "User";
        var localPart = email.Split('@')[0];
        var words = localPart
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(Capitalise);
        return string.Join(' ', words);
    }

    private static string Capitalise(string word) =>
        char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
}
