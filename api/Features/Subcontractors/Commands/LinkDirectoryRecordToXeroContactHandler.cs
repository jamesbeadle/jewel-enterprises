using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Writes the one row that makes an existing directory record "linked to Xero": a
/// SubcontractorXeroLink from the record to the Xero contact, the same row an import writes.
/// Nothing else on the record moves unless the caller asked to pull Xero's details (2026-09-09,
/// <see cref="XeroDetailsPull"/>). Both sides must be free — a record already holding a link,
/// or a contact already linked to another record, is refused with who holds it, because silently
/// re-pointing a link is how a company's bills end up reconciled against the wrong supplier.
/// </summary>
public sealed class LinkDirectoryRecordToXeroContactHandler : ICommandHandler<LinkDirectoryRecordToXeroContact, Subcontractor>
{
    private readonly JpmsContext context;
    private readonly XeroSupplierLookup xeroSuppliers;
    private readonly AuditActor actor;
    private readonly AuditTrail audit;

    public LinkDirectoryRecordToXeroContactHandler(JpmsContext context, XeroSupplierLookup xeroSuppliers, AuditActor actor, AuditTrail audit)
    {
        this.context = context;
        this.xeroSuppliers = xeroSuppliers;
        this.actor = actor;
        this.audit = audit;
    }

    public async Task<Subcontractor> HandleAsync(LinkDirectoryRecordToXeroContact command, CancellationToken cancellationToken)
    {
        var record = await context.Subcontractors
            .FirstOrDefaultAsync(sub => sub.SubcontractorId == command.SubcontractorId, cancellationToken)
            ?? throw new InvalidOperationException("That directory record does not exist.");
        if (record.IsProspect)
            throw new InvalidOperationException(
                $"{record.CompanyName} is a tender-only prospect — promote it into the Directory before linking it to Xero.");

        await RefuseIfEitherSideLinkedAsync(record, command.XeroContactId, cancellationToken);

        var supplier = await xeroSuppliers.FindAsync(command.XeroContactId, cancellationToken);

        var link = new SubcontractorXeroLinkEntity
        {
            SubcontractorXeroLinkId = SubcontractorIdentifierFactory.NextSubcontractorXeroLinkId(),
            SubcontractorId = record.SubcontractorId,
            XeroContactId = supplier.ContactId,
            XeroContactName = supplier.Name,
            ImportedAt = DateTimeOffset.UtcNow,
            ImportedByEmail = actor.Email
        };
        context.SubcontractorXeroLinks.Add(link);
        if (command.PullDetailsFromXero) await PullDetailsAsync(record, supplier, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            AuditEventType.DirectoryRecordXeroLinkChanged,
            $"{record.CompanyName} linked to Xero contact {supplier.Name} ({supplier.ContactId})"
            + (command.PullDetailsFromXero ? ", Xero's contact details pulled onto the record." : "."),
            cancellationToken: cancellationToken);

        return record.ToModel(
            await context.TradesForAsync(record.SubcontractorId, cancellationToken),
            xeroLinked: true,
            xeroLinks: new[] { link.ToModel() });
    }

    private async Task PullDetailsAsync(SubcontractorEntity record, XeroSupplier supplier, CancellationToken cancellationToken)
    {
        var contacts = await context.CompanyContacts
            .Where(contact => contact.SubcontractorId == record.SubcontractorId)
            .ToListAsync(cancellationToken);
        var before = contacts.Count;
        XeroDetailsPull.Apply(record, contacts, supplier, DateTimeOffset.UtcNow);
        context.CompanyContacts.AddRange(contacts.Skip(before));
    }

    private async Task RefuseIfEitherSideLinkedAsync(SubcontractorEntity record, string xeroContactId, CancellationToken cancellationToken)
    {
        var recordsOwnLink = await context.SubcontractorXeroLinks.AsNoTracking()
            .FirstOrDefaultAsync(link => link.SubcontractorId == record.SubcontractorId, cancellationToken);
        if (recordsOwnLink is not null)
            throw new InvalidOperationException(
                $"{record.CompanyName} is already linked to Xero contact {recordsOwnLink.XeroContactName}. Unlink it first if that is wrong.");

        var contactsExistingLink = await context.SubcontractorXeroLinks.AsNoTracking()
            .FirstOrDefaultAsync(link => link.XeroContactId == xeroContactId, cancellationToken);
        if (contactsExistingLink is null) return;

        var holder = await context.Subcontractors.AsNoTracking()
            .Where(sub => sub.SubcontractorId == contactsExistingLink.SubcontractorId)
            .Select(sub => sub.CompanyName)
            .FirstOrDefaultAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Xero contact {contactsExistingLink.XeroContactName} is already linked to {holder ?? contactsExistingLink.SubcontractorId}. Unlink it there first, or consolidate the two records.");
    }
}
