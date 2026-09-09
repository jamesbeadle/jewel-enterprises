using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Contracts.Subcontractors;

// Pushing a directory record's contacts to its linked Xero contact (2026-09-09, the accountant's
// ask: he added two people on Wilson Electric in the portal and then typed them into Xero again).
// The push is something a person presses, never automatic. It is planned the same way twice —
// the preview a person confirms and the write that follows — by one rule: the record's primary
// contact becomes Xero's primary person, the record's company contacts become Xero's additional
// persons (Xero replaces them wholesale, and holds at most five), and an EMPTY side on the portal
// never clears what Xero has. Names go to Xero as first name + last name, split on the first space.

/// <summary>What the push would do: Xero's people now, the people after, and anything to know first.</summary>
public sealed record PreviewXeroContactPush(string SubcontractorId) : IQuery<XeroContactPushPreview>;

public sealed record XeroContactPushPreview(
    XeroContactPeople Now,
    XeroContactPeople After,
    IReadOnlyList<string> Warnings)
{
    public bool ChangesAnything =>
        !Equals(Now.PrimaryPerson, After.PrimaryPerson) || !Now.AdditionalPersons.SequenceEqual(After.AdditionalPersons);
}

/// <summary>Writes the record's contacts onto its linked Xero contact, exactly as the preview showed. Audited.</summary>
public sealed record PushDirectoryContactsToXeroContact(string SubcontractorId) : ICommand<XeroContactPushOutcome>;

public sealed record XeroContactPushOutcome(string XeroContactName, XeroContactPeople Written);
