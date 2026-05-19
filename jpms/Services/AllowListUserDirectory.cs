using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Hard-coded list of approved users for the initial build. Replace with a real
/// API call once the admin-managed directory exists.
/// Emails are matched case-insensitively.
/// </summary>
public sealed class AllowListUserDirectory : IUserDirectory
{
    private static readonly IReadOnlyList<DirectoryUser> Approved = new[]
    {
        new DirectoryUser("nigel.reilly@jewelgroup.co.uk", "Nigel Reilly",        "Managing Director"),
        new DirectoryUser("admin@jewelgroup.co.uk",       "Jewel Admin",          "Administrator"),
        new DirectoryUser("accountant@jewelgroup.co.uk",  "Jewel Accountant",     "Accountant"),
        new DirectoryUser("qs@jewelgroup.co.uk",          "Jewel QS",             "Quantity Surveyor"),
    };

    public DirectoryUser? Find(string email) =>
        string.IsNullOrWhiteSpace(email)
            ? null
            : Approved.FirstOrDefault(u =>
                string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
}
