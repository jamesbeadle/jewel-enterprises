using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;

/// <summary>Pushing people to Xero is held to the Xero-link gate — the same people who may decide the link.</summary>
public sealed class PushDirectoryContactsToXeroContactAuthorisation
{
    public bool Allows(SignedInUser user, PushDirectoryContactsToXeroContact command) =>
        DirectoryXeroLinkRoles.MayLink.IncludesAny(user.Roles);
}

public sealed class PushDirectoryContactsToXeroContactValidation
{
    public ValidationOutcome Check(PushDirectoryContactsToXeroContact command) =>
        string.IsNullOrWhiteSpace(command.SubcontractorId)
            ? ValidationOutcome.Failed("SubcontractorId is required.")
            : ValidationOutcome.Passed;
}
