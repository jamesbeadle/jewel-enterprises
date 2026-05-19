using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Looks up whether a signed-in email belongs to a user the company has approved
/// for the platform. The shape of this interface is deliberately backend-agnostic
/// so the implementation can swap from a hard-coded allow-list to a real API call
/// without changing any UI code.
/// </summary>
public interface IUserDirectory
{
    /// <summary>
    /// Returns the directory record for an email, or null if the email is not
    /// on the approved list.
    /// </summary>
    DirectoryUser? Find(string email);

    /// <summary>True if the email is on the approved list.</summary>
    bool IsApproved(string email) => Find(email) is not null;
}
