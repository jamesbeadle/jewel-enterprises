using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// The narrow back-fill for an invoice raised in Xero by hand (2026-09-10, the accountant's ask:
/// VI-0002 to VI-0005 were keyed into Xero before the portal could raise them). Stamps the Xero
/// number — and XeroRaisedAt when it was blank — on an invoice at any status but Cancelled,
/// leaves XeroInvoiceId null because the portal did not raise it, and writes nothing to Xero.
/// Refused on an invoice the portal itself raised: that number is Xero's own answer.
/// </summary>
public sealed class RecordValuationInvoiceXeroNumberHandler : ICommandHandler<RecordValuationInvoiceXeroNumber, ValuationInvoice>
{
    private readonly JpmsContext context;
    public RecordValuationInvoiceXeroNumberHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(RecordValuationInvoiceXeroNumber command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.Status == (int)ValuationInvoiceStatus.Cancelled)
            throw new InvalidOperationException("A cancelled valuation invoice has no Xero invoice to record.");
        if (!string.IsNullOrWhiteSpace(entity.XeroInvoiceId))
            throw new InvalidOperationException($"This valuation invoice was raised in Xero by the portal as {entity.XeroInvoiceNumber} — its number cannot be replaced.");

        var number = command.XeroInvoiceNumber.Trim();
        var previous = entity.XeroInvoiceNumber;
        entity.XeroInvoiceNumber = number;
        entity.XeroRaisedAt ??= DateTimeOffset.UtcNow;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId, ValuationInvoiceEventType.RaisedInXero,
            (string.IsNullOrWhiteSpace(previous)
                ? $"Xero invoice number {number} recorded (raised in Xero by hand)"
                : $"Xero invoice number changed from {previous} to {number} (raised in Xero by hand)")
            + $" by {command.RecordedBy ?? "the portal"}.",
            amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
