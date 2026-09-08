using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Deletes one SubcontractorXeroLink row — the undo of a link or of a mistaken import's link.
/// The record and everything referencing it stay; only the Xero pairing goes, and the audit row
/// says who took it off and from which contact.
/// </summary>
public sealed class UnlinkDirectoryRecordFromXeroContactHandler : ICommandHandler<UnlinkDirectoryRecordFromXeroContact, Subcontractor>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public UnlinkDirectoryRecordFromXeroContactHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<Subcontractor> HandleAsync(UnlinkDirectoryRecordFromXeroContact command, CancellationToken cancellationToken)
    {
        var record = await context.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(sub => sub.SubcontractorId == command.SubcontractorId, cancellationToken)
            ?? throw new InvalidOperationException("That directory record does not exist.");

        var link = await context.SubcontractorXeroLinks
            .FirstOrDefaultAsync(row => row.SubcontractorId == record.SubcontractorId && row.XeroContactId == command.XeroContactId, cancellationToken)
            ?? throw new InvalidOperationException($"{record.CompanyName} is not linked to that Xero contact.");

        context.SubcontractorXeroLinks.Remove(link);
        await context.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            AuditEventType.DirectoryRecordXeroLinkChanged,
            $"{record.CompanyName} unlinked from Xero contact {link.XeroContactName} ({link.XeroContactId}).",
            cancellationToken: cancellationToken);

        var remainingLinks = await DirectoryXeroLinks.ForRecordAsync(context, record.SubcontractorId, cancellationToken);
        return record.ToModel(
            await context.TradesForAsync(record.SubcontractorId, cancellationToken),
            xeroLinked: remainingLinks.Count > 0,
            xeroLinks: remainingLinks);
    }
}
