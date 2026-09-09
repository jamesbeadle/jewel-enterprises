using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Adds or updates a person on a directory record's contact list. A null/blank CompanyContactId
/// inserts; a populated one updates in place (scoped to the record, so a contact can never be
/// edited onto a different company).
/// </summary>
public sealed class UpsertCompanyContactHandler : ICommandHandler<UpsertCompanyContact, CompanyContact>
{
    private readonly JpmsContext context;

    public UpsertCompanyContactHandler(JpmsContext context) { this.context = context; }

    public async Task<CompanyContact> HandleAsync(UpsertCompanyContact command, CancellationToken cancellationToken)
    {
        var recordExists = await context.Subcontractors
            .AnyAsync(sub => sub.SubcontractorId == command.SubcontractorId, cancellationToken);
        if (!recordExists)
            throw new InvalidOperationException($"Directory record '{command.SubcontractorId}' not found.");

        CompanyContactEntity? entity = null;
        if (!string.IsNullOrWhiteSpace(command.CompanyContactId))
        {
            entity = await context.CompanyContacts.FirstOrDefaultAsync(
                contact => contact.CompanyContactId == command.CompanyContactId
                    && contact.SubcontractorId == command.SubcontractorId,
                cancellationToken);
            if (entity is null)
                throw new InvalidOperationException("That contact was not found on this record.");
        }

        if (entity is null)
        {
            entity = new CompanyContactEntity
            {
                CompanyContactId = SubcontractorIdentifierFactory.NextCompanyContactId(),
                SubcontractorId = command.SubcontractorId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.CompanyContacts.Add(entity);
        }

        entity.Name = command.Name.Trim();
        entity.Email = command.Email.Trim();
        entity.Phone = command.Phone.Trim();

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
