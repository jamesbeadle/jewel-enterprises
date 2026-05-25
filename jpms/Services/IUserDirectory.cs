using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IUserDirectory
{
    DirectoryUser? Find(string email);

    bool IsApproved(string email) => Find(email) is not null;

    IReadOnlyList<DirectoryUser> All();

    DirectoryUser Upsert(DirectoryUser user);

    bool Remove(string email);

    event Action? OnChange;
}
