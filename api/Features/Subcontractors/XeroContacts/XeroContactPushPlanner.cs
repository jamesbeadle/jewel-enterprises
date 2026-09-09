using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;

/// <summary>
/// The one rule behind the contact push (2026-09-09), used by the preview and the write alike so
/// a person confirms exactly what is sent: the record's primary contact becomes Xero's primary
/// person and its company contacts become Xero's additional persons — but a blank primary or an
/// empty contact list on the portal leaves Xero's own people as they are (the accountant's
/// condition: an empty portal list never clears Xero's), and Xero holds at most five additional
/// persons, so the rest are left off and the warning says which.
/// </summary>
public static class XeroContactPushPlanner
{
    public static (XeroContactPeople After, IReadOnlyList<string> Warnings) Plan(
        SubcontractorEntity record, IReadOnlyList<CompanyContactEntity> contacts, XeroContactPeople now)
    {
        var warnings = new List<string>();
        var primary = PrimaryFor(record, now, warnings);
        var additional = AdditionalFor(contacts, now, warnings);
        return (now with { PrimaryPerson = primary, AdditionalPersons = additional }, warnings);
    }

    private static XeroContactPerson? PrimaryFor(SubcontractorEntity record, XeroContactPeople now, List<string> warnings)
    {
        if (!string.IsNullOrWhiteSpace(record.ContactName))
            return new XeroContactPerson(record.ContactName.Trim(), record.ContactEmail.Trim());
        if (now.PrimaryPerson is not null)
            warnings.Add("The record has no primary contact, so Xero's primary person is left as it is.");
        return now.PrimaryPerson;
    }

    private static IReadOnlyList<XeroContactPerson> AdditionalFor(
        IReadOnlyList<CompanyContactEntity> contacts, XeroContactPeople now, List<string> warnings)
    {
        var named = contacts
            .Where(contact => !string.IsNullOrWhiteSpace(contact.Name))
            .Select(contact => new XeroContactPerson(contact.Name.Trim(), contact.Email.Trim()))
            .ToList();
        if (named.Count == 0)
        {
            if (now.AdditionalPersons.Count > 0)
                warnings.Add("The record has no other contacts, so Xero's additional people are left as they are.");
            return now.AdditionalPersons;
        }
        if (named.Count <= XeroContactPeople.AdditionalPersonsLimit) return named;

        var leftOff = named.Skip(XeroContactPeople.AdditionalPersonsLimit).Select(person => person.Name);
        warnings.Add($"Xero holds at most {XeroContactPeople.AdditionalPersonsLimit} additional people — {string.Join(", ", leftOff)} will be left off.");
        return named.Take(XeroContactPeople.AdditionalPersonsLimit).ToList();
    }
}
