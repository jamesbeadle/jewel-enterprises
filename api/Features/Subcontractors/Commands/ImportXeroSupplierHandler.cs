using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Copies one Xero supplier into the company directory: a new record (category Supplier, no trades
/// — those are curated by hand afterwards), a SubcontractorXeroLink row marking the record linked
/// to Xero, and one company contact row per Xero contact person. Importing never merges into an
/// existing record — duplicates are resolved afterwards through ConsolidateDirectoryRecords, so
/// there is one consistent flow for all duplicates — and a supplier that already HAS a record is
/// linked to it with LinkDirectoryRecordToXeroContact instead of imported (the modal offers that
/// when the names match).
/// </summary>
public sealed class ImportXeroSupplierHandler : ICommandHandler<ImportXeroSupplier, Subcontractor>
{
    private readonly JpmsContext context;
    private readonly XeroSupplierLookup xeroSuppliers;
    private readonly AuditActor actor;
    private readonly AuditTrail audit;

    public ImportXeroSupplierHandler(JpmsContext context, XeroSupplierLookup xeroSuppliers, AuditActor actor, AuditTrail audit)
    {
        this.context = context;
        this.xeroSuppliers = xeroSuppliers;
        this.actor = actor;
        this.audit = audit;
    }

    public async Task<Subcontractor> HandleAsync(ImportXeroSupplier command, CancellationToken cancellationToken)
    {
        // The unique index backs this up, but checking first gives the caller a message rather
        // than a database error when the same supplier is imported twice (e.g. two open tabs).
        var alreadyLinked = await context.SubcontractorXeroLinks
            .AnyAsync(link => link.XeroContactId == command.XeroContactId, cancellationToken);
        if (alreadyLinked)
            throw new InvalidOperationException("That Xero supplier has already been imported into the directory.");

        var supplier = await xeroSuppliers.FindAsync(command.XeroContactId, cancellationToken);

        var entity = new SubcontractorEntity
        {
            SubcontractorId = SubcontractorIdentifierFactory.NextSubcontractorId(),
            CompanyName = supplier.Name,
            ContactName = XeroContacts.XeroDetailsPull.PrimaryPersonOf(supplier),
            ContactEmail = supplier.EmailAddress,
            ContactPhone = supplier.Phone,
            CisStatus = "",
            OnboardedAt = DateTimeOffset.UtcNow,
            Category = (int)DirectoryCategory.Supplier,
            MobileNumber = supplier.Mobile,
            Town = supplier.Town,
            County = supplier.County,
            AddressLine = supplier.AddressLine,
            Postcode = supplier.Postcode
        };
        context.Subcontractors.Add(entity);

        var link = new SubcontractorXeroLinkEntity
        {
            SubcontractorXeroLinkId = SubcontractorIdentifierFactory.NextSubcontractorXeroLinkId(),
            SubcontractorId = entity.SubcontractorId,
            XeroContactId = supplier.ContactId,
            XeroContactName = supplier.Name,
            ImportedAt = DateTimeOffset.UtcNow,
            ImportedByEmail = actor.Email
        };
        context.SubcontractorXeroLinks.Add(link);

        // Xero's additional contact persons come across as company contacts, so nothing Xero
        // holds about who to talk to is lost. Xero's own primary person seeds the primary line
        // above (2026-09-09 — it was never read before, so Tom Dix never came across); the first
        // additional person only stands in when the contact has none.
        foreach (var person in supplier.ContactPersons)
        {
            context.CompanyContacts.Add(new CompanyContactEntity
            {
                CompanyContactId = SubcontractorIdentifierFactory.NextCompanyContactId(),
                SubcontractorId = entity.SubcontractorId,
                Name = person.Name,
                Email = person.EmailAddress,
                Phone = "",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Auto-link matching workers (2026-08-31, the accountant's month-end doc, item A):
        // settlement is gated on the worker→directory link, and importing the supplier used to
        // create everything EXCEPT it — the one row the labour machinery actually keys on. Same
        // name rule as the allocation page's recognition (WorkerDirectoryMatcher), unambiguous
        // matches only, and never over a link or sole-trader flag a human has already set.
        var unlinkedWorkers = await context.Workers
            .Where(worker => worker.IsActive && worker.SubcontractorId == null && !worker.IsSoleTrader)
            .ToListAsync(cancellationToken);
        var existingCompanies = unlinkedWorkers.Count == 0
            ? new List<string>()
            : await context.Subcontractors.AsNoTracking()
                .Where(sub => !sub.IsProspect && sub.SubcontractorId != entity.SubcontractorId)
                .Select(sub => sub.CompanyName)
                .ToListAsync(cancellationToken);
        var autoLinked = new List<string>();
        foreach (var worker in unlinkedWorkers)
        {
            if (!WorkerDirectoryMatcher.Matches(worker.Name, supplier.Name)) continue;
            // Unambiguous only — the same standard the reconcile sweep holds itself to: a worker
            // whose name also matches another directory company is a human's call, not an import
            // side effect.
            if (existingCompanies.Any(company => WorkerDirectoryMatcher.Matches(worker.Name, company))) continue;
            worker.SubcontractorId = entity.SubcontractorId;
            autoLinked.Add(worker.Name);
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var name in autoLinked)
            await audit.WriteAsync(
                AuditEventType.WorkerLinkedToDirectory,
                $"Xero import: worker {name} auto-linked to {supplier.Name}.",
                cancellationToken: cancellationToken);

        return entity.ToModel(Array.Empty<Trade>(), xeroLinked: true, xeroLinks: new[] { link.ToModel() });
    }
}
