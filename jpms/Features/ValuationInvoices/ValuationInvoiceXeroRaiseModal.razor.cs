using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Features.ValuationInvoices;

public partial class ValuationInvoiceXeroRaiseModal
{
    /// <summary>Raised once the invoice is issued — by the Xero raise or without it — with a
    /// one-line result for the page to show. The page re-reads its invoices and certified totals.</summary>
    [Parameter] public EventCallback<string> OnIssued { get; set; }

    private bool open;
    private string valuationInvoiceId = "";
    private string reference = "";
    private ValuationInvoiceXeroRaisePreview? preview;
    private string? error;
    private bool raising;
    private bool issuing;

    public void Open(ValuationInvoice invoice)
    {
        valuationInvoiceId = invoice.ValuationInvoiceId;
        reference = invoice.DisplayNumber.Length > 0 ? invoice.DisplayNumber : invoice.Reference;
        preview = null;
        error = null;
        open = true;
        _ = LoadPreviewAsync();
        StateHasChanged();
    }

    private void Close()
    {
        if (raising || issuing) return;
        open = false;
    }

    private async Task LoadPreviewAsync()
    {
        try { preview = await Invoices.PreviewXeroRaiseAsync(valuationInvoiceId); }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't read what Xero would hold. Please try again."; }
        finally { StateHasChanged(); }
    }

    private async Task RaiseAsync()
    {
        if (raising || preview is null || !preview.CanRaise) return;
        error = null;
        try
        {
            raising = true;
            var outcome = await Invoices.RaiseInXeroAsync(valuationInvoiceId);
            open = false;
            await OnIssued.InvokeAsync(ResultLine(outcome));
        }
        catch (CommandFailedException ex) { error = $"Couldn't raise in Xero: {ex.Message}"; }
        catch { error = "Couldn't raise the invoice in Xero. Please try again."; }
        finally { raising = false; }
    }

    private async Task IssueWithoutXeroAsync()
    {
        if (issuing) return;
        error = null;
        try
        {
            issuing = true;
            await Invoices.IssueAsync(valuationInvoiceId);
            open = false;
            await OnIssued.InvokeAsync($"{reference} issued — nothing raised in Xero.");
        }
        catch (CommandFailedException ex) { error = $"Couldn't issue: {ex.Message}"; }
        catch { error = "Couldn't issue the invoice. Please try again."; }
        finally { issuing = false; }
    }

    private string ResultLine(ValuationInvoiceXeroRaiseOutcome outcome)
    {
        var attachment = outcome.CertificateAttached
            ? "certificate attached"
            : outcome.AttachmentError is null ? "no certificate to attach" : $"certificate NOT attached: {outcome.AttachmentError}";
        return $"{reference} raised in Xero as {outcome.XeroInvoiceNumber} — net {Money(outcome.Net)}, VAT {Money(outcome.Tax)}, total {Money(outcome.Total)}; {attachment}. {outcome.TaxNote}";
    }
}
