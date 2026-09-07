using System.Globalization;

namespace Jewel.JPMS.Components;

public partial class ValuationInvoicesSection
{
    private async Task AddAsync()
    {
        if (busy) return;
        error = null;
        // Historic convenience: if only the Paid box was filled in, that figure IS the
        // invoice amount (a fully paid historic invoice) — don't make people type it twice.
        if (newIsHistoric && string.IsNullOrWhiteSpace(newAmount) && !string.IsNullOrWhiteSpace(newPaidAmount))
            newAmount = newPaidAmount;
        if (!TryParseAmount(newAmount, out var amount)) { error = "Enter an invoice amount greater than zero in Amount £."; return; }
        decimal? paidAmount = null;
        if (newIsHistoric && !string.IsNullOrWhiteSpace(newPaidAmount))
        {
            // 0 = issued but never paid; blank = fully paid.
            if (!TryParsePaid(newPaidAmount, amount, "Enter a paid amount (0 = unpaid), or leave it blank for fully paid.", out var paid)) return;
            paidAmount = paid;
        }
        try
        {
            busy = true;
            await CreateNewAsync(amount, paidAmount);
            newAmount = newPaidAmount = newNote = "";
            await ReloadAsync();
            if (newIsHistoric) await OnCertifiedChanged.InvokeAsync();
        }
        // A refused raise (the claim is still a draft, or already has a live invoice) arrives as
        // the server's own sentence — print it, not the generic retry line.
        catch (CommandFailedException refusal) { error = refusal.Message; }
        catch { error = "Couldn't add the valuation invoice. Please try again."; }
        finally { busy = false; }
    }

    // A historic entry is backdated to the period month server-side and counts toward
    // certified immediately; a live one is drawn against the selected claim.
    private Task<ValuationInvoice> CreateNewAsync(decimal amount, decimal? paidAmount) =>
        newIsHistoric
            ? Invoices.CreateManualAsync(ProjectId, ParseMonth(newMonth), amount,
                paidAmount ?? amount, issuedAt: null, paidAt: null, note: NoteOrNull(newNote))
            : Invoices.CreateAsync(ProjectId, ParseMonth(newMonth), amount, ValuationClaimId);

    private void OpenReject(ValuationInvoice invoice)
    {
        rejectInvoice = invoice;
        rejectReason = "";
    }

    private void OpenEdit(ValuationInvoice invoice)
    {
        editInvoice = invoice;
        editMonth = invoice.PeriodMonth.ToString("yyyy-MM");
        editAmount = invoice.Amount.ToString(CultureInfo.InvariantCulture);
        editPaidAmount = invoice.AmountPaid > 0 ? invoice.AmountPaid.ToString(CultureInfo.InvariantCulture) : "";
        editNote = "";
    }

    private async Task SaveEditAsync()
    {
        if (busy || editInvoice is null) return;
        error = null;
        if (!TryParseEditAmount(editInvoice, out var amount)) return;
        if (!TryParseEditPaid(editInvoice, amount, out var paidAmount)) return;
        var invoice = editInvoice;
        var certifiedMoves = invoice.IsManual;
        editInvoice = null;
        try
        {
            busy = true;
            await Invoices.UpdateAsync(invoice.ValuationInvoiceId, ParseMonth(editMonth), amount,
                amountPaid: paidAmount, note: NoteOrNull(editNote));
            await ReloadAsync();
            if (certifiedMoves) await OnCertifiedChanged.InvokeAsync();
        }
        catch { error = "Couldn't save the invoice. Please try again."; }
        finally { busy = false; }
    }

    // Manual entries may be zeroed — correcting a mistaken historic figure without losing the
    // row. Workflow invoices still need a real amount.
    private bool TryParseEditAmount(ValuationInvoice invoice, out decimal amount)
    {
        if (TryParseAmount(editAmount, out amount, allowZero: invoice.IsManual)) return true;
        error = invoice.IsManual
            ? "Enter an amount of zero or more (0 voids the invoice's value)."
            : "Enter an amount greater than zero.";
        return false;
    }

    // Zeroing the invoice zeroes its receipts with it — the paid total rolls back on save.
    // Otherwise a manual entry's typed paid figure travels; blank leaves the receipts alone.
    private bool TryParseEditPaid(ValuationInvoice invoice, decimal amount, out decimal? paidAmount)
    {
        paidAmount = null;
        if (amount == 0m) { paidAmount = 0m; return true; }
        if (!invoice.IsManual || string.IsNullOrWhiteSpace(editPaidAmount)) return true;
        if (!TryParsePaid(editPaidAmount, amount, "Enter a valid paid amount, or leave it blank.", out var paid)) return false;
        paidAmount = paid;
        return true;
    }

    private void OpenPayment(ValuationInvoice invoice)
    {
        paymentInvoice = invoice;
        paymentAmount = invoice.Amount.ToString(CultureInfo.InvariantCulture);
    }

    private bool TryParsePaid(string text, decimal amount, string invalidMessage, out decimal paid)
    {
        if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out paid) || paid < 0)
        { error = invalidMessage; return false; }
        if (paid > amount) { error = "Paid amount can't exceed the invoice amount."; return false; }
        return true;
    }

    private static bool TryParseAmount(string value, out decimal amount, bool allowZero = false) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out amount)
        && (allowZero ? amount >= 0 : amount > 0);

    private static DateTimeOffset ParseMonth(string value) =>
        DateTimeOffset.TryParse(string.IsNullOrWhiteSpace(value) ? "" : value + "-01", out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

    private static string? NoteOrNull(string note) => string.IsNullOrWhiteSpace(note) ? null : note;
}
