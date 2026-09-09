namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// The people on one Xero contact, as Xero holds them right now: the contact's own primary
/// person (Xero's top-level FirstName / LastName / EmailAddress) and its additional persons
/// (ContactPersons, at most five). Read fresh by id, never from the cached supplier list, so a
/// push's "now" is true at the moment it is shown.
/// </summary>
public sealed record XeroContactPeople(
    string ContactId,
    string Name,
    XeroContactPerson? PrimaryPerson,
    IReadOnlyList<XeroContactPerson> AdditionalPersons)
{
    public const int AdditionalPersonsLimit = 5;
}
