using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;

namespace Jewel.JPMS.Pages;

public partial class ProjectValuation
{

    // ---- Panel-driven invoice actions --------------------------------------
    // The claim card is where the FD works the whole flow, so the invoice moves are
    // first-class here — the row menu in Valuation Invoices stays for the edge cases.

    // The selected claim's live invoice — what the card's primary button acts on.
    private ValuationInvoice? SelectedInvoice => Selected is { } claim ? InvoiceFor(claim) : null;

    // Records that the claim has gone to the architect/client (Raised → Submitted, "Awaiting
    // approval"). A record of an outside event, like Record approval / Record payment — the
    // portal emails nothing here; the snapshot's Email action drafts the statement if wanted.
    private Task RecordClaimSentAsync()
    {
        if (busy || SelectedInvoice is not { } invoice) return Task.CompletedTask;
        return GuardAsync(async () =>
        {
            await Invoices.SubmitAsync(invoice.ValuationInvoiceId);
            await ReloadInvoicePanelsAsync();
            // An amended invoice freezes a fresh snapshot when re-claimed — refresh the register.
            OnCertifiedChanged();
        }, "Couldn't record the claim as sent — the server may be restarting. Please try again.");
    }

    private Task ApproveInvoiceAsync()
    {
        if (busy || SelectedInvoice is not { } invoice) return Task.CompletedTask;
        return GuardAsync(async () =>
        {
            await Invoices.ApproveAsync(invoice.ValuationInvoiceId);
            await ReloadInvoicePanelsAsync();
        }, "Couldn't record the approval — the server may be restarting. Please try again.");
    }

    private Task IssueInvoiceAsync()
    {
        if (busy || SelectedInvoice is not { } invoice) return Task.CompletedTask;
        return GuardAsync(async () =>
        {
            await Invoices.IssueAsync(invoice.ValuationInvoiceId);
            await ReloadInvoicePanelsAsync();
            // Issuing moves certified to date — re-pull claims for the re-frozen totals.
            OnCertifiedChanged();
        }, "Couldn't issue the invoice — the server may be restarting. Please try again.");
    }

    // The moves that need a form (amount, reason) open the invoices section's own modals —
    // they render whether or not the accordion is open, so nothing is duplicated here.
    private void AmendRejectedInvoice() { if (SelectedInvoice is { } invoice) invoicesSection?.OpenEditFor(invoice); }
    private void OpenRejectInvoice() { if (SelectedInvoice is { } invoice) invoicesSection?.OpenRejectFor(invoice); }
    private void OpenRecordPayment() { if (SelectedInvoice is { } invoice) invoicesSection?.OpenPaymentFor(invoice); }

    // Refresh both readers of the invoice list: the section's table (which reports back via
    // OnInvoicedToDateChanged, updating our stage copy) — or our copy directly before the
    // section has rendered.
    private async Task ReloadInvoicePanelsAsync()
    {
        if (invoicesSection is not null) await invoicesSection.ReloadAsync();
        else await RefreshInvoicesAsync();
    }

    // A failed API call (validation, or the server mid-deploy answering 503) surfaces in
    // the banner instead of tearing the whole app down with an unhandled exception.
    private string? actionError;

    private async Task GuardAsync(Func<Task> action, string failureMessage)
    {
        actionError = null;
        busy = true;
        try { await action(); }
        catch (CommandFailedException failure) { actionError = failure.Message; }
        catch { actionError = failureMessage; }
        finally { busy = false; }
    }

    private Task PreapproveAsync() => Selected is null ? Task.CompletedTask : GuardAsync(
        () => Store.PreapproveClaimAsync(ProjectId, Selected.ValuationClaimId),
        "Couldn't lock the claim — the server may be restarting. Please try again.");

    // Nudge, don't enforce: if nothing drawn against this claim has been issued or paid,
    // ask before confirming — certified to date won't include this claim otherwise. If
    // the invoice list can't be checked, don't block the confirm on it.
    private async Task ConfirmClickedAsync()
    {
        if (Selected is null || busy) return;
        bool hasIssuedInvoice;
        try
        {
            var invoices = await Invoices.ListAsync(ProjectId);
            hasIssuedInvoice = invoices.Any(invoice =>
                invoice.ValuationClaimId == Selected.ValuationClaimId
                && invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid);
        }
        catch { hasIssuedInvoice = true; }
        if (!hasIssuedInvoice) { showConfirmNudge = true; return; }
        await ConfirmAsync();
    }

    private Task ConfirmAsync()
    {
        if (Selected is null) return Task.CompletedTask;
        showConfirmNudge = false;
        var confirmed = Selected;
        // Decide BEFORE the store reloads: is this the newest claim, i.e. has nothing been
        // rolled over from it yet?
        var rollOver = IsLatestClaim(confirmed);
        return GuardAsync(async () =>
        {
            await Store.ConfirmClaimAsync(ProjectId, confirmed.ValuationClaimId);
            // Roll straight over: offer the next period seeded from the claim just confirmed.
            // Only once — a claim that already has a later period behind it was rolled over
            // when that period was started; the server has just re-based that draft on this
            // claim's figures, and opening the form again would only create a duplicate.
            if (rollOver) OpenNextClaimForm(confirmed);
        }, "Couldn't confirm the claim — the server may be restarting. Please try again.");
    }

    // Only the newest claim offers "Start next claim" — older ones would create
    // out-of-sequence duplicates of periods that already exist.
    private bool IsLatestClaim(ValuationClaim claim) =>
        LatestClaim?.ValuationClaimId == claim.ValuationClaimId;

    // Opens the start-claim modal with the following month suggested as the new period's
    // name. Used after Confirm and by the card's "Start next claim" button on an
    // already-confirmed claim. Seeding from the latest claim is automatic.
    private void OpenNextClaimForm(ValuationClaim baseline)
    {
        newClaimName = baseline.ClaimDate.AddMonths(1).ToString("MMMM yyyy", Gb);
        newClaimDate = DateTime.Today;
        showStartClaim = true;
    }

    // Undo an unintended "We're claiming this": Preapproved → Draft.
    private Task ReopenAsync() => Selected is null ? Task.CompletedTask : GuardAsync(
        () => Store.ReopenClaimAsync(ProjectId, Selected.ValuationClaimId),
        "Couldn't reopen the claim — the server may be restarting. Please try again.");

    private void OpenAddLine()
    {
        editingLine = null;
        showLineForm = true;
    }

    private void StartEditLine(ValuationLineItem line)
    {
        showLineForm = false;
        editingLine = line;
    }

    private void CloseLineForm()
    {
        showLineForm = false;
        editingLine = null;
    }

    // After an edit, close the modal. After an add, keep it open so several lines can be entered in a row.
    private void OnLineSaved()
    {
        if (editingLine is not null) editingLine = null;
    }

    public void Dispose()
    {
        Store.OnChange -= OnStoreChanged;
        Retention.OnChange -= OnStoreChanged;
        Projects.OnChanged -= OnStoreChanged;
    }
}
