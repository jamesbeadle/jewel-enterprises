using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>
/// The raise, in the order that loses nothing: plan (every blocker named first, nothing touched),
/// raise the AUTHORISED invoice in Xero, stamp its id on the valuation invoice and SAVE — so a
/// failure after this point can never leave an invoice in Xero the portal does not know about —
/// then attach the certificate (best effort, the outcome says), then the existing Issue move.
/// </summary>
public sealed class RaiseValuationInvoiceInXeroHandler : ICommandHandler<RaiseValuationInvoiceInXero, ValuationInvoiceXeroRaiseOutcome>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;
    private readonly XeroOptions options;
    private readonly IDocumentControlBlobStore blobs;
    private readonly ICommandHandler<IssueValuationInvoice, ValuationInvoice> issue;

    public RaiseValuationInvoiceInXeroHandler(
        JpmsContext context, IXeroClient xero, XeroOptions options, IDocumentControlBlobStore blobs,
        ICommandHandler<IssueValuationInvoice, ValuationInvoice> issue)
    {
        this.context = context;
        this.xero = xero;
        this.options = options;
        this.blobs = blobs;
        this.issue = issue;
    }

    public async Task<ValuationInvoiceXeroRaiseOutcome> HandleAsync(RaiseValuationInvoiceInXero command, CancellationToken cancellationToken)
    {
        var plan = await new ValuationInvoiceXeroRaisePlanner(context, xero, options).PlanAsync(command.ValuationInvoiceId, cancellationToken);
        if (!plan.ToPreview().CanRaise)
            throw new InvalidOperationException(string.Join(" ", plan.Blockers));

        var raised = await xero.CreateSalesInvoiceAsync(plan.Request, cancellationToken);
        if (!raised.Succeeded || string.IsNullOrWhiteSpace(raised.InvoiceId))
            throw new InvalidOperationException(raised.Error ?? "Xero did not raise the invoice.");

        await StampAsync(command, raised, cancellationToken);
        var (attached, attachmentError) = await AttachCertificateAsync(plan, raised.InvoiceId, cancellationToken);
        var invoice = await issue.HandleAsync(new IssueValuationInvoice(command.ValuationInvoiceId), cancellationToken);

        return new ValuationInvoiceXeroRaiseOutcome(
            raised.InvoiceId, raised.InvoiceNumber ?? "", raised.SubTotal, raised.TotalTax, raised.Total,
            raised.Note, attached, attachmentError, invoice);
    }

    private async Task StampAsync(RaiseValuationInvoiceInXero command, XeroSalesInvoiceResult raised, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.SingleAsync(row => row.ValuationInvoiceId == command.ValuationInvoiceId, cancellationToken);
        entity.XeroInvoiceId = raised.InvoiceId;
        entity.XeroInvoiceNumber = raised.InvoiceNumber;
        entity.XeroRaisedAt = DateTimeOffset.UtcNow;
        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId, ValuationInvoiceEventType.RaisedInXero,
            $"Raised in Xero as {raised.InvoiceNumber} by {command.RaisedBy ?? "the portal"} — total £{raised.Total:N2}. {raised.Note}",
            amountAfter: entity.Amount);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<(bool Attached, string? Error)> AttachCertificateAsync(ValuationInvoiceXeroRaisePlan plan, string xeroInvoiceId, CancellationToken cancellationToken)
    {
        if (plan.Certificate is null) return (false, null);
        var blob = await blobs.OpenAsync(plan.Certificate.BlobRef, cancellationToken);
        if (blob is null) return (false, $"The certificate file {plan.Certificate.FileName} is no longer in storage.");

        using var buffer = new MemoryStream();
        await using (blob.Content) await blob.Content.CopyToAsync(buffer, cancellationToken);
        var result = await xero.AttachToInvoiceAsync(
            xeroInvoiceId, plan.Certificate.FileName, blob.ContentType, buffer.ToArray(), cancellationToken);
        if (result.Succeeded) return (true, null);

        ValuationInvoiceAuditTrail.Append(context, plan.Invoice.ValuationInvoiceId, ValuationInvoiceEventType.RaisedInXero,
            $"Certificate {plan.Certificate.FileName} could not be attached in Xero: {result.Error}");
        await context.SaveChangesAsync(cancellationToken);
        return (false, result.Error);
    }
}
