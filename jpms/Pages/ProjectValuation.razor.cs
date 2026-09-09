using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;

namespace Jewel.JPMS.Pages;

public partial class ProjectValuation
{
    [Parameter] public string ProjectId { get; set; } = "";

    // Session checked and the user is signed in. This is NOT "the report is here" — keeping the
    // two apart is the point: the heading and the New claim button show at once, each panel holds
    // until everything behind it can be shown together.
    private bool busy;
    private bool showStartClaim;
    private bool showLineForm;
    private bool showClientReferences;
    private string selectedClaimId = "";
    private ValuationLineItem? editingLine;

    // Certification runs GROSS of the deposit: each issued/paid invoice's cash amount plus
    // the deposit credit embedded in it. Both figures come from our own invoice list, which
    // the invoices section keeps fresh via OnInvoicedToDateChanged below.
    private IEnumerable<ValuationInvoice> CertifiableInvoices => (projectInvoices ?? Array.Empty<ValuationInvoice>())
        .Where(invoice => invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid);

    private decimal CertifiedToDateGross => CertifiableInvoices.Sum(invoice => invoice.CertifiedAmount);

    private decimal DepositCreditedToDate => CertifiableInvoices.Sum(invoice => invoice.DepositCredited);

    // Fired by the invoices section on every reload — keep our copy of the invoice list in
    // step so the claim card's stage (invoice raised? issued? paid?) and the certified
    // figures track reality. The raw total is superseded by CertifiedToDateGross above.
    private async Task OnInvoicedToDateChanged(decimal total)
    {
        await RefreshInvoicesAsync();
        StateHasChanged();
    }

    // ---- Claim stage (drives the card's hint, primary button and stepper) ---
    // Nullable on purpose. No invoices yet is a real answer — it puts the claim at "awaiting
    // invoice" — so "not fetched yet" has to be a state the card can tell apart.
    private IReadOnlyList<ValuationInvoice>? projectInvoices;

    // A failed fetch must open the gate, or the jewel pulses forever; the stage then falls back
    // to the claim status alone, as it always did.
    private bool invoicesFailed;

    // -- Panel gates. Each lists every source the panel reads, so it appears in one piece. --
    private bool ClaimReady =>
        Store.ReportLoadedFor(ProjectId) && (projectInvoices is not null || invoicesFailed);

    private bool ReportReady => Store.ReportLoadedFor(ProjectId) && CostCenters.IsLoaded;

    private async Task RefreshInvoicesAsync()
    {
        try { projectInvoices = await Invoices.ListAsync(ProjectId); }
        catch { invoicesFailed = true; /* stage falls back to claim status alone */ }
    }

    // The live invoice drawn against this claim (latest wins; cancelled ones don't count).
    private ValuationInvoice? InvoiceFor(ValuationClaim claim) => (projectInvoices ?? Array.Empty<ValuationInvoice>())
        .Where(invoice => invoice.ValuationClaimId == claim.ValuationClaimId
                          && invoice.Status != ValuationInvoiceStatus.Cancelled)
        .OrderByDescending(invoice => invoice.Number)
        .FirstOrDefault();

    // One stage per invoice status, so the card can always name the ONE most likely next
    // action — the FD's whole flow (send → approve → issue → paid) runs off this panel
    // instead of the row dropdown in Valuation Invoices below.
    private enum ClaimStage { Draft, AwaitingInvoice, InvoiceDraft, AwaitingApproval, ApprovedAwaitingIssue, InvoiceRejected, AwaitingPayment, ReadyToConfirm, Confirmed }

    private ClaimStage StageFor(ValuationClaim claim)
    {
        if (claim.Status == ValuationClaimStatus.Confirmed) return ClaimStage.Confirmed;
        if (claim.Status == ValuationClaimStatus.Draft) return ClaimStage.Draft;
        var invoice = InvoiceFor(claim);
        if (invoice is null) return ClaimStage.AwaitingInvoice;
        return invoice.Status switch
        {
            ValuationInvoiceStatus.Paid => ClaimStage.ReadyToConfirm,
            ValuationInvoiceStatus.Issued => ClaimStage.AwaitingPayment,
            ValuationInvoiceStatus.Approved => ClaimStage.ApprovedAwaitingIssue,
            ValuationInvoiceStatus.Submitted => ClaimStage.AwaitingApproval,
            ValuationInvoiceStatus.Rejected => ClaimStage.InvoiceRejected,
            _ => ClaimStage.InvoiceDraft // Raised — a draft that was never sent (legacy, or a manually added invoice)
        };
    }

    private string StageHint(ValuationClaim claim)
    {
        var invoice = InvoiceFor(claim);
        return StageFor(claim) switch
        {
            ClaimStage.Draft =>
                "Set each line's cumulative % complete (Bulk edit % handles many at once), then lock the claim.",
            // The handover: the project team's part is done; from here the card is accounts'.
            ClaimStage.AwaitingInvoice =>
                $"Valued and locked by the project team — {Money(claim.TotalWorksComplete)} works complete. Over to accounts: raise the invoice for the amount due. Raising files it as a draft and freezes the report behind it — nothing is sent from here.",
            ClaimStage.InvoiceDraft =>
                $"Invoice {invoice?.DisplayNumber} raised for {Money(invoice?.Amount ?? 0m)}, not yet claimed. Send the frozen report to the architect/client as usual (Report Snapshots can draft the email), then record the claim as sent — or, if this client runs no approval loop, issue it directly from Actions.",
            ClaimStage.AwaitingApproval =>
                $"Claimed — invoice {invoice?.DisplayNumber} for {Money(invoice?.Amount ?? 0m)} is with the architect/client{(invoice?.SubmittedAt is { } sub ? $" since {sub:dd MMM yyyy}" : "")}. Record their approval (or rejection, in Actions) when it comes.",
            ClaimStage.ApprovedAwaitingIssue =>
                $"Approved — issue invoice {invoice?.DisplayNumber} to count it toward certified to date, then put it through the accounts as usual.",
            ClaimStage.InvoiceRejected =>
                $"Invoice {invoice?.DisplayNumber} was rejected{(invoice?.RejectedAt is { } rej ? $" on {rej:dd MMM yyyy}" : "")} — amend it (back to draft, ready to claim again), or cancel it in Valuation Invoices below.",
            ClaimStage.AwaitingPayment =>
                $"Invoice {invoice?.DisplayNumber} issued for {Money(invoice?.Amount ?? 0m)} — payment is no gate: carry on with the next claim and record the payment (Actions) when the cash lands.",
            ClaimStage.ReadyToConfirm => IsLatestClaim(claim)
                ? $"Invoice {invoice?.DisplayNumber} is paid — confirm to lock this period as the baseline and roll into the next."
                : $"Invoice {invoice?.DisplayNumber} is paid — confirm to lock this period as the baseline the next claim measures against. It has already been rolled over, so nothing new is started.",
            ClaimStage.Confirmed =>
                $"Confirmed{(claim.ConfirmedAt is { } at ? $" on {at:dd MMM yyyy}" : "")} — this claim is the baseline the next period starts from.",
            _ => ""
        };
    }

    private sealed record ClaimStep(int Index, string Label, bool Done, bool Current);

    private IEnumerable<ClaimStep> StepsFor(ValuationClaim claim)
    {
        var locked = claim.Status != ValuationClaimStatus.Draft;
        var invoice = InvoiceFor(claim);
        // A rejected invoice reads as "not claimed": the Claim step lights up again, which is
        // exactly where the FD is — amend and resend. Issued-without-approval marks Approve
        // done implicitly; the audit trail keeps the distinction.
        var claimed = invoice is { Status: ValuationInvoiceStatus.Submitted or ValuationInvoiceStatus.Approved or ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid };
        var approved = invoice is { Status: ValuationInvoiceStatus.Approved or ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid };
        var invoiced = invoice is { Status: ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid };
        var paid = invoice is { Status: ValuationInvoiceStatus.Paid };
        var confirmed = claim.Status == ValuationClaimStatus.Confirmed;
        yield return new(1, "Value & lock", locked, !locked);
        yield return new(2, "Claim", claimed || confirmed, locked && !claimed && !confirmed);
        yield return new(3, "Approve", approved || confirmed, claimed && !approved && !confirmed);
        yield return new(4, "Invoice", invoiced || confirmed, approved && !invoiced && !confirmed);
        yield return new(5, "Paid", paid || confirmed, invoiced && !paid && !confirmed);
        yield return new(6, ConfirmLabel(claim), confirmed, paid && !confirmed);
    }

    // Rolling over is a once-only step: it seeds the next period from this claim. Once a
    // later claim exists this claim HAS been rolled over, so its step, button and Actions
    // item read plain "Confirm" — confirming still matters (it is what makes this claim the
    // baseline the later draft measures against), but nothing new is started from it.
    private string ConfirmLabel(ValuationClaim claim) =>
        IsLatestClaim(claim) ? "Confirm & roll over" : "Confirm";


    // Certified to date moved (invoice issued / deleted / added as paid): the server has
    // re-frozen any Preapproved claim's totals, so re-pull claims to show them.
    private void OnCertifiedChanged() => Store.Refresh(ProjectId);

    // Read-only viewer for a frozen report snapshot ("show me the report behind VI-0007").
    private string? viewingSnapshotId;
    private void OpenSnapshot(string snapshotId) => viewingSnapshotId = snapshotId;
    private void CloseSnapshot() => viewingSnapshotId = null;

    // Email-draft flow for a snapshot — closes the viewer (one modal at a time) and opens the
    // draft modal on the snapshot's cached header row. Same circle as the snapshot register's
    // take/delete gate: the roles that run the report, not everyone who may read it.
    private ValuationReportSnapshot? emailingSnapshot;
    private bool CanEmailSnapshot => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager);

    // Who may map cost centres to the client's references — the bill's drafters plus the FD,
    // who reconciles with the client. Mirrors ValuationReportAuthorisation on the API.
    private bool CanMapClientReferences => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager or Role.QuantitySurveyor);

    // Who may manage the claim itself (rename, reopen, early confirm, delete): the roles
    // that run the report — Admin, MD, FD, PM — never the client, who only reads it.
    private bool CanManageClaims => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager);

    // Who may record % complete — exactly the API's gate for claim entries (Director, FD, PM,
    // QS, plus administrators); the same set ModalCatalog.ClaimProgress carries.
    private bool CanRecordEntries => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager or Role.QuantitySurveyor);

    // The claim card's Actions menu. Everything secondary or destructive for the selected
    // claim reads as one list here — the card itself only ever shows the ONE next action.
    private List<DropdownMenu.Item> ClaimMenuItems
    {
        get
        {
            var items = new List<DropdownMenu.Item>();
            if (Selected is not { } claim) return items;

            // Group 0 — naming.
            items.Add(new(Label: "Rename claim…",
                OnSelect: EventCallback.Factory.Create(this, OpenRename),
                Hint: "Name the period this claim values — it shows everywhere the claim does"));

            // Group 0 — the % column, batched: the same act as typing into the report, one save
            // for many lines, and the dialog the assistant fills (claim_progress).
            if (claim.Status == ValuationClaimStatus.Draft && CanRecordEntries)
                items.Add(new(Label: "Set % complete…",
                    OnSelect: EventCallback.Factory.Create(this, OpenClaimProgress),
                    Hint: "Enter cumulative % complete for several lines at once — or have the assistant fill it from the evidence"));

            // Group 1 — walking the stage back (or jumping it forward) on a preapproved claim.
            if (claim.Status == ValuationClaimStatus.Preapproved)
            {
                items.Add(new(Label: "Reopen as draft",
                    OnSelect: EventCallback.Factory.Create(this, ReopenAsync),
                    Hint: "Un-issue the claim — back to Draft, % complete editable again",
                    Disabled: busy, Group: 1));
                if (StageFor(claim) != ClaimStage.ReadyToConfirm)
                    items.Add(new(Label: ConfirmLabel(claim),
                        OnSelect: EventCallback.Factory.Create(this, ConfirmClickedAsync),
                        Hint: "Normally done once the claim's invoice is paid — confirming now will ask first",
                        Disabled: busy, Group: 1));
            }

            // Group 1 (continued) — the other scenarios for the invoice stage the card is at.
            // The most likely move is the card's primary button; these are the alternatives.
            switch (StageFor(claim))
            {
                case ClaimStage.InvoiceDraft:
                    items.Add(new(Label: "Issue without approval…",
                        OnSelect: EventCallback.Factory.Create(this, OpenXeroRaise),
                        Hint: "For clients with no formal approval loop — raise in Xero (or issue without) and count toward certified to date",
                        Disabled: busy, Group: 1));
                    break;
                case ClaimStage.AwaitingApproval:
                    items.Add(new(Label: "Record rejection…",
                        OnSelect: EventCallback.Factory.Create(this, OpenRejectInvoice),
                        Hint: "The client refused the claim — unlocks the invoice for amendment or cancellation",
                        Disabled: busy, Group: 1));
                    items.Add(new(Label: "Issue without approval…",
                        OnSelect: EventCallback.Factory.Create(this, OpenXeroRaise),
                        Hint: "For clients with no formal approval loop — raise in Xero (or issue without) and count toward certified to date",
                        Disabled: busy, Group: 1));
                    break;
                case ClaimStage.AwaitingPayment:
                    items.Add(new(Label: "Record payment…",
                        OnSelect: EventCallback.Factory.Create(this, OpenRecordPayment),
                        Hint: "The cash has landed — rolls the amount into the project's paid total",
                        Disabled: busy, Group: 1));
                    break;
            }

            // Group 2 — the destructive tail.
            items.Add(new(Label: "Delete claim…",
                OnSelect: EventCallback.Factory.Create(this, () => showDeleteClaim = true),
                Hint: "Delete this claim and its entries — invoices and snapshots survive with the link cleared",
                Destructive: true, Group: 2));
            return items;
        }
    }

    private void OpenSnapshotEmail(string snapshotId)
    {
        viewingSnapshotId = null;
        emailingSnapshot = Store.SnapshotsFor(ProjectId)
            .FirstOrDefault(snapshot => snapshot.ValuationReportSnapshotId == snapshotId);
    }

    // Deleting a snapshot clears any invoice's link to it server-side — reload the
    // invoice section so a dead "View report" link doesn't linger.
    private ValuationInvoicesSection? invoicesSection;
    private async Task OnSnapshotDeleted()
    {
        if (invoicesSection is not null) await invoicesSection.ReloadAsync();
    }

    private DateTime newClaimDate = DateTime.Today;
    private string newClaimName = "";

    private bool showRename;
    private string renameValue = "";
    private bool showDeleteClaim;
    // Nudge (not a gate): confirming a claim with no issued/paid linked invoice asks first.
    private bool showConfirmNudge;

    private static readonly System.Globalization.CultureInfo Gb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
    private static string PctText(decimal v) => v.ToString("0.##", Gb) + "%";

    private IReadOnlyList<ValuationClaim> Claims => Store.ClaimsFor(ProjectId);
    private ValuationClaim? Selected => Claims.FirstOrDefault(c => c.ValuationClaimId == selectedClaimId);
    private int NextClaimNumber => Claims.Count == 0 ? 1 : Claims.Max(c => c.ClaimNumber) + 1;
    private ValuationClaim? LatestClaim => Claims.OrderByDescending(c => c.ClaimNumber).FirstOrDefault();

    // The claim's payment due as the report currently computes it (frozen for a locked claim,
    // live for a draft) — the figure the "Raise invoice" button offers to bill.
    private decimal PaymentDueNow => Selected is null
        ? 0m
        : ValuationSummaryFigures.For(
            Store.LinesFor(ProjectId),
            Store.EntriesFor(Selected.ValuationClaimId),
            Selected, CertifiedToDateGross, DepositCreditedToDate).PaymentDueExVat;

    // The claim immediately before the selected one (highest ClaimNumber below it), so the
    // report table can show each line's previously-claimed % against what's being claimed now.
    // Null for Claim 1 (nothing came before) or when no claim is selected.
    private ValuationClaim? PreviousClaim => Selected is null
        ? null
        : Claims.Where(c => c.ClaimNumber < Selected.ClaimNumber)
                .OrderByDescending(c => c.ClaimNumber)
                .FirstOrDefault();
}
