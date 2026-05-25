using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IAccessRequestStore
{
    IReadOnlyList<AccessRequest> Pending();

    AccessRequest Submit(AuthenticatedUser user);

    bool Remove(string email);

    event Action? OnChange;
}
