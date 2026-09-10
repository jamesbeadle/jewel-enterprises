using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Approved -> Issued, or Raised/Submitted -> Issued for projects that skip the formal approval
/// loop (invoices are claimed — Submitted — at raise now, so the skip path starts there; Raised
/// survives for drafts and legacy rows). From here the amount counts toward "Certified to date".
/// A report snapshot is normally frozen at raise; issuing re-freezes only when no live one backs
/// the invoice (amended since raise, or pre-dating raise-time capture), so even one-click
/// invoices keep the report behind them. A XeroInvoiceNumber on the command is an invoice raised
/// in Xero BY HAND (2026-09-10): the number and the time it was recorded are stamped, XeroInvoiceId
/// stays null (the portal did not raise it), and the row reads as raised from then on.
/// </summary>
public sealed class IssueValuationInvoiceHandler : ICommandHandler<IssueValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public IssueValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(IssueValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");

        switch ((ValuationInvoiceStatus)entity.Status)
        {
            case ValuationInvoiceStatus.Raised:
            case ValuationInvoiceStatus.Submitted: // claimed, but this client runs no formal approval loop
            case ValuationInvoiceStatus.Approved:
                break; // the legal starting points
            case ValuationInvoiceStatus.Rejected:
                throw new InvalidOperationException("This valuation invoice was rejected — amend and resubmit it first.");
            case ValuationInvoiceStatus.Cancelled:
                throw new InvalidOperationException("A cancelled valuation invoice cannot be issued.");
            case ValuationInvoiceStatus.Issued:
                throw new InvalidOperationException("This valuation invoice has already been issued.");
            case ValuationInvoiceStatus.Paid:
                throw new InvalidOperationException("A paid valuation invoice cannot be re-issued.");
        }

        // Make sure a LIVE report snapshot backs the invoice: raise-time capture normally
        // guarantees one, but an invoice amended since (snapshot flagged superseded) or raised
        // before raise-time capture existed needs a fresh freeze of the current ask.
        var hasLiveSnapshot = await context.ValuationReportSnapshots
            .AnyAsync(snapshot => snapshot.ValuationInvoiceId == entity.ValuationInvoiceId
                                  && !snapshot.IsSuperseded, cancellationToken);
        if (!hasLiveSnapshot)
        {
            var snapshot = await ValuationReportSnapshotCapture.CaptureAsync(
                context, entity.ProjectId, $"{entity.Reference} issue", entity.ValuationInvoiceId, cancellationToken);
            entity.ValuationReportSnapshotId = snapshot.ValuationReportSnapshotId;
        }

        // The audit trail says when the approval loop was skipped — the trail is the only
        // place that distinction survives once the status reads Issued.
        var note = entity.Status == (int)ValuationInvoiceStatus.Submitted
            ? "Issued without a recorded approval."
            : "";

        var handRaisedNumber = string.IsNullOrWhiteSpace(command.XeroInvoiceNumber) ? null : command.XeroInvoiceNumber.Trim();
        if (handRaisedNumber is not null)
        {
            if (!string.IsNullOrWhiteSpace(entity.XeroInvoiceId))
                throw new InvalidOperationException($"This valuation invoice was raised in Xero by the portal as {entity.XeroInvoiceNumber} — its number cannot be replaced.");
            entity.XeroInvoiceNumber = handRaisedNumber;
            entity.XeroRaisedAt ??= DateTimeOffset.UtcNow;
            note = $"{note} Raised in Xero by hand as {handRaisedNumber}.".Trim();
        }

        entity.Status = (int)ValuationInvoiceStatus.Issued;
        entity.IssuedAt = DateTimeOffset.UtcNow;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Issued, note, amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);

        // Issuing raises "Certified to date" — re-freeze any Preapproved claim's totals so
        // the report summary reflects it (e.g. seeding historical claims under a claim
        // that was preapproved before the invoices existed).
        await PreapprovedClaimTotals.RefreshAsync(context, entity.ProjectId, cancellationToken);

        return entity.ToModel();
    }
}
