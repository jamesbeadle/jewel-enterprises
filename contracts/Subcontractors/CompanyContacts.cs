using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>The additional people on a company directory record (see <see cref="CompanyContact"/>).</summary>
public sealed record ListCompanyContacts(string SubcontractorId) : IQuery<IReadOnlyList<CompanyContact>>;

/// <summary>
/// Adds or updates a person on a directory record's contact list. A null/blank CompanyContactId
/// inserts; a populated one updates in place.
/// </summary>
public sealed record UpsertCompanyContact(
    string SubcontractorId,
    string Name,
    string Email,
    string Phone,
    string? CompanyContactId = null) : ICommand<CompanyContact>;

public sealed record RemoveCompanyContact(string SubcontractorId, string CompanyContactId)
    : ICommand<Acknowledgement>;
