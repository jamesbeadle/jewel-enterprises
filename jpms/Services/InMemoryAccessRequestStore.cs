using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryAccessRequestStore : IAccessRequestStore
{
    private readonly List<AccessRequest> requests = new();

    public event Action? OnChange;

    public IReadOnlyList<AccessRequest> Pending() =>
        requests
            .OrderByDescending(request => request.RequestedAt)
            .ToList()
            .AsReadOnly();

    public AccessRequest Submit(AuthenticatedUser user)
    {
        var existing = FindByEmail(user.Email);
        if (existing is not null) requests.Remove(existing);

        var request = new AccessRequest(
            user.Email,
            user.DisplayName,
            user.Provider,
            DateTimeOffset.UtcNow);

        requests.Add(request);
        OnChange?.Invoke();
        return request;
    }

    public bool Remove(string email)
    {
        var existing = FindByEmail(email);
        if (existing is null) return false;
        requests.Remove(existing);
        OnChange?.Invoke();
        return true;
    }

    private AccessRequest? FindByEmail(string email) =>
        requests.FirstOrDefault(request =>
            string.Equals(request.Email, email, StringComparison.OrdinalIgnoreCase));
}
