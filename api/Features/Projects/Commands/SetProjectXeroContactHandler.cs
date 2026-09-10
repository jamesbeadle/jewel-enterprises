using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

/// <summary>
/// Stores the Xero customer a project's sales invoices are raised on. The contact is read back
/// from Xero by id before anything is written: a contact Xero does not hold is refused, and the
/// stored name is Xero's own — never the caller's — so Raise in Xero always lands on a real
/// contact under the name the ledger knows it by. Null clears the mapping.
/// </summary>
public sealed class SetProjectXeroContactHandler
    : ICommandHandler<SetProjectXeroContact, Project>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;

    public SetProjectXeroContactHandler(JpmsContext context, IXeroClient xero)
    {
        this.context = context;
        this.xero = xero;
    }

    public async Task<Project> HandleAsync(SetProjectXeroContact command, CancellationToken cancellationToken)
    {
        var entity = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var contactId = command.XeroContactId?.Trim();
        if (string.IsNullOrEmpty(contactId))
        {
            entity.XeroContactId = null;
            entity.XeroContactName = null;
        }
        else
        {
            var lookup = await xero.LookupSalesContactAsync(contactId, cancellationToken);
            switch (lookup.Status)
            {
                case XeroSalesContactStatus.Found:
                    entity.XeroContactId = contactId;
                    entity.XeroContactName = lookup.XeroName ?? "";
                    break;
                case XeroSalesContactStatus.NotFound:
                    throw new InvalidOperationException(
                        $"Xero has no contact with id {contactId} — pick one from list_xero_customers (or the Xero contacts list in Project settings).");
                default:
                    throw new InvalidOperationException(
                        $"Xero could not be read to verify the contact ({lookup.TaxNote}); try again shortly.");
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
