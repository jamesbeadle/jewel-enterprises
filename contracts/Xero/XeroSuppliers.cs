using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// Asks the API for the contacts held in Xero, for the directory's "Import from Xero" modal. ALL
/// active contacts are returned — not just those Xero flags IsSupplier — because that flag only
/// turns on once a contact has had a bill, which would hide a supplier created in Xero moments
/// ago; the IsSupplier/IsCustomer flags come back on each row so the modal can hide customer-only
/// contacts by default. Each contact is also stamped with whether a directory record is already
/// linked to it. The API caches the Xero read briefly to respect Xero's rate limits;
/// <paramref name="Force"/> bypasses that cache for an explicit user refresh.
/// </summary>
public sealed record ListXeroSuppliers(bool Force = false) : IQuery<XeroSuppliersSnapshot>;

/// <summary>
/// What the API saw when it asked Xero for suppliers. Mirrors <see cref="XeroTransactionsSnapshot"/>:
/// <see cref="IsConfigured"/> false = no Xero credentials (the UI explains rather than erroring);
/// <see cref="Error"/> carries a human-readable failure when Xero itself said no;
/// <see cref="FetchedAtUtc"/> is when Xero was actually read (older than 'now' when cached);
/// <see cref="Truncated"/> true = the page cap was hit with suppliers left unfetched.
/// </summary>
public sealed record XeroSuppliersSnapshot(
    bool IsConfigured,
    string? Error,
    DateTimeOffset? FetchedAtUtc,
    bool Truncated,
    IReadOnlyList<XeroSupplier> Suppliers)
{
    public static XeroSuppliersSnapshot NotConfigured() =>
        new(false, null, null, false, Array.Empty<XeroSupplier>());

    public static XeroSuppliersSnapshot Failed(string error) =>
        new(true, error, null, false, Array.Empty<XeroSupplier>());
}

/// <summary>
/// One contact as Xero holds it. Phone fields are assembled from Xero's structured phone rows
/// (country + area + number); <see cref="Town"/>/<see cref="County"/> come from the contact's first
/// address with a city (Xero City/Region). <see cref="IsSupplier"/>/<see cref="IsCustomer"/> are
/// Xero's own flags (set once a contact has had a bill / an invoice; a brand-new contact carries
/// neither). <see cref="AlreadyImported"/> and <see cref="LinkedSubcontractorId"/> are stamped by
/// the API from the directory's Xero links — the Xero client itself leaves them at their defaults.
/// </summary>
public sealed record XeroSupplier(
    string ContactId,
    string Name,
    string EmailAddress,
    string Phone,
    string Mobile,
    string Town,
    string County,
    string AddressLine,
    string Postcode,
    IReadOnlyList<XeroContactPerson> ContactPersons,
    bool IsSupplier = false,
    bool IsCustomer = false,
    bool AlreadyImported = false,
    string? LinkedSubcontractorId = null,
    string? MatchingSubcontractorId = null,
    string MatchingSubcontractorName = "");

/// <summary>An additional person on a Xero contact (Xero's ContactPersons list).</summary>
public sealed record XeroContactPerson(string Name, string EmailAddress);
