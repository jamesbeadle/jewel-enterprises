using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;

/// <summary>
/// The opt-in half of linking (2026-09-09): copy what Xero holds about a contact onto an EXISTING
/// directory record. A field moves only where Xero has a value — a blank in Xero never wipes a
/// corrected detail — and Xero's additional persons become company contacts where the record
/// has nobody by that email (or, lacking an email, that name). The company name stays: the link
/// was made by name, and renaming is "Edit details". The primary contact is Xero's primary
/// person, else its first additional person, as the import reads it.
/// </summary>
public static class XeroDetailsPull
{
    public static void Apply(SubcontractorEntity record, List<CompanyContactEntity> contacts, XeroSupplier supplier, DateTimeOffset now)
    {
        Take(PrimaryPersonOf(supplier), value => record.ContactName = value);
        Take(supplier.EmailAddress, value => record.ContactEmail = value);
        Take(supplier.Phone, value => record.ContactPhone = value);
        Take(supplier.Mobile, value => record.MobileNumber = value);
        Take(supplier.AddressLine, value => record.AddressLine = value);
        Take(supplier.Town, value => record.Town = value);
        Take(supplier.County, value => record.County = value);
        Take(supplier.Postcode, value => record.Postcode = value);
        foreach (var person in supplier.ContactPersons.Where(person => !HasAlready(contacts, person)))
            contacts.Add(new CompanyContactEntity
            {
                CompanyContactId = SubcontractorIdentifierFactory.NextCompanyContactId(),
                SubcontractorId = record.SubcontractorId,
                Name = person.Name,
                Email = person.EmailAddress,
                Phone = "",
                CreatedAt = now
            });
    }

    /// <summary>Xero's primary person, else the first additional person — the same reading the import makes.</summary>
    public static string PrimaryPersonOf(XeroSupplier supplier) =>
        !string.IsNullOrWhiteSpace(supplier.PrimaryPersonName) ? supplier.PrimaryPersonName
        : supplier.ContactPersons.Count > 0 ? supplier.ContactPersons[0].Name
        : "";

    private static void Take(string value, Action<string> assign)
    {
        if (!string.IsNullOrWhiteSpace(value)) assign(value.Trim());
    }

    private static bool HasAlready(List<CompanyContactEntity> contacts, XeroContactPerson person) =>
        contacts.Any(contact => !string.IsNullOrWhiteSpace(person.EmailAddress)
            ? contact.Email.Equals(person.EmailAddress, StringComparison.OrdinalIgnoreCase)
            : contact.Name.Equals(person.Name, StringComparison.OrdinalIgnoreCase));
}
