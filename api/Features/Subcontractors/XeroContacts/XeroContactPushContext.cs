using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;

/// <summary>Everything a push needs, loaded once for the preview and once for the write: the
/// record, its link, its company contacts and the people Xero holds right now.</summary>
public sealed record XeroContactPushContext(
    SubcontractorEntity Record,
    SubcontractorXeroLinkEntity Link,
    IReadOnlyList<CompanyContactEntity> Contacts,
    XeroContactPeople Now)
{
    public static async Task<XeroContactPushContext> LoadAsync(
        JpmsContext context, IXeroClient xero, string subcontractorId, CancellationToken cancellationToken)
    {
        var record = await context.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(sub => sub.SubcontractorId == subcontractorId, cancellationToken)
            ?? throw new InvalidOperationException("That directory record does not exist.");
        var link = await context.SubcontractorXeroLinks.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.SubcontractorId == subcontractorId, cancellationToken)
            ?? throw new InvalidOperationException($"{record.CompanyName} is not linked to a Xero contact — link it first.");
        var contacts = await context.CompanyContacts.AsNoTracking()
            .Where(contact => contact.SubcontractorId == subcontractorId)
            .OrderBy(contact => contact.CreatedAt)
            .ToListAsync(cancellationToken);
        var now = await ReadNowAsync(xero, link, cancellationToken);
        return new XeroContactPushContext(record, link, contacts, now);
    }

    private static async Task<XeroContactPeople> ReadNowAsync(IXeroClient xero, SubcontractorXeroLinkEntity link, CancellationToken cancellationToken)
    {
        try
        {
            return await xero.GetContactPeopleAsync(link.XeroContactId, cancellationToken)
                ?? throw new InvalidOperationException($"Xero has no contact {link.XeroContactName} ({link.XeroContactId}) any more — check the link.");
        }
        catch (XeroCallFailedException failure)
        {
            throw new InvalidOperationException($"Couldn't read the contact from Xero: {failure.Message}");
        }
    }
}
