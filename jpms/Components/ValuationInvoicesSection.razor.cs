namespace Jewel.JPMS.Components;

public partial class ValuationInvoicesSection
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";
    // When a claim is selected on the report, new invoices are drawn against it.
    [Parameter] public string? ValuationClaimId { get; set; }
    [Parameter] public EventCallback<decimal> OnInvoicedToDateChanged { get; set; }

    /// <summary>Raised after a change that moves "Certified to date" (issue, delete,
    /// manual add/edit). The server re-freezes any Preapproved claim's totals at the same
    /// time, so the parent should refresh its claims to show them.</summary>
    [Parameter] public EventCallback OnCertifiedChanged { get; set; }

    /// <summary>Opens the read-only report-snapshot viewer for the given snapshot id —
    /// "show me the valuation report behind this invoice".</summary>
    [Parameter] public EventCallback<string> OnViewSnapshot { get; set; }

    private bool isOpen;
    private bool busy;
    private string? error;
    private IReadOnlyList<ValuationInvoice> invoices = Array.Empty<ValuationInvoice>();

    private string newMonth = DateTimeOffset.UtcNow.ToString("yyyy-MM");
    private string newAmount = "";
    // Historic entry: recorded directly as Issued/Paid, backdated to the period month.
    private bool newIsHistoric;
    private string newPaidAmount = "";
    private string newNote = "";

    private ValuationInvoice? paymentInvoice;
    private string paymentAmount = "";

    // "Record Xero number…" (2026-09-10): the invoice whose hand-raised Xero number is being recorded.
    private ValuationInvoice? xeroNumberInvoice;
    private string xeroNumberText = "";
    private string? pendingDeleteId;

    // The invoice whose Actions menu is currently open — while set, the table's overflow cap is
    // lifted so the menu isn't clipped (see the wrapper div). Cleared on reload in case the row
    // disappears under an open menu.
    private string? openMenuId;

    private ValuationInvoice? rejectInvoice;
    private string rejectReason = "";

    private ValuationInvoice? editInvoice;
    private string editMonth = "";
    private string editAmount = "";
    private string editPaidAmount = "";
    private string editNote = "";

    private string? historyOpenId;
    private IReadOnlyList<ValuationInvoiceEvent>? historyEvents;

    private int ColumnCount => CanManage ? 7 : 6;

    private bool CanManage => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager);

    // Cancelled invoices stay listed (audit trail) but never count.
    private IReadOnlyList<ValuationInvoice> LiveInvoices =>
        invoices.Where(invoice => invoice.Status != ValuationInvoiceStatus.Cancelled).ToList();

    // Issued + Paid invoices are what the client has actually been invoiced — this is the
    // figure that feeds "Certified to date" on the report summary.
    private decimal InvoicedToDate => LiveInvoices
        .Where(invoice => invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid)
        .Sum(invoice => invoice.Amount);

    private decimal TotalPaid => LiveInvoices.Sum(invoice => invoice.AmountPaid);

    // Submitted + Approved: claimed from the client but not yet certifiable.
    private decimal AwaitingApproval => LiveInvoices
        .Where(invoice => invoice.IsAwaitingApproval)
        .Sum(invoice => invoice.Amount);

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private void Toggle() => isOpen = !isOpen;

    /// <summary>Re-pulls the invoice list. Public so the page can nudge it after an
    /// out-of-band change — e.g. deleting a snapshot clears invoice links server-side.</summary>
    public async Task ReloadAsync()
    {
        openMenuId = null;
        try
        {
            invoices = await Invoices.ListAsync(ProjectId);
            error = null;
        }
        catch { error = "Couldn't load valuation invoices. Please try again."; }
        await OnInvoicedToDateChanged.InvokeAsync(InvoicedToDate);
        StateHasChanged();
    }

    /// <summary>Open this section's modals for a given invoice from outside — the claim card
    /// on the valuation page drives the happy path and borrows these forms rather than
    /// duplicating them. The modals render whether or not the accordion is open.</summary>
    public void OpenPaymentFor(ValuationInvoice invoice) { OpenPayment(invoice); StateHasChanged(); }
    public void OpenRejectFor(ValuationInvoice invoice) { OpenReject(invoice); StateHasChanged(); }
    public void OpenEditFor(ValuationInvoice invoice) { OpenEdit(invoice); StateHasChanged(); }
}
