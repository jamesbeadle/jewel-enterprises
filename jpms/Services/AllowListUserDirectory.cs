using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class AllowListUserDirectory : IUserDirectory
{
    private readonly List<DirectoryUser> users = new()
    {
        new DirectoryUser("jamesbeadle1989@gmail.com",     "James Beadle",      Role.Admin),
        new DirectoryUser("admin@jewelgroup.co.uk",        "Jewel Admin",       Role.Admin),
        new DirectoryUser("nigel.reilly@jewelgroup.co.uk", "Nigel Reilly",      Role.ManagingDirector),
        new DirectoryUser("accountant@jewelgroup.co.uk",   "Jewel Accountant",  Role.Accountant),
        new DirectoryUser("qs@jewelgroup.co.uk",           "Jewel QS",          Role.QuantitySurveyor),
    };

    public event Action? OnChange;

    public DirectoryUser? Find(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return users.FirstOrDefault(user =>
            string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<DirectoryUser> All() => users.AsReadOnly();

    public DirectoryUser Upsert(DirectoryUser user)
    {
        var existing = Find(user.Email);
        if (existing is not null) users.Remove(existing);
        users.Add(user);
        OnChange?.Invoke();
        return user;
    }

    public bool Remove(string email)
    {
        var existing = Find(email);
        if (existing is null) return false;
        users.Remove(existing);
        OnChange?.Invoke();
        return true;
    }
}
