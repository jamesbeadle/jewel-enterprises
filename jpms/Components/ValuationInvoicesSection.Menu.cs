namespace Jewel.JPMS.Components;

public partial class ValuationInvoicesSection
{
    private List<DropdownMenu.Item> InvoiceMenuItems(ValuationInvoice invoice) =>
        LifecycleItems(invoice).Concat(AmendItems(invoice)).Concat(RecordItems(invoice)).ToList();

    private IEnumerable<DropdownMenu.Item> LifecycleItems(ValuationInvoice invoice) => invoice.Status switch
    {
        ValuationInvoiceStatus.Raised when !invoice.IsManual => new[]
        {
            Item("Record claim sent", () => SubmitAsync(invoice), "The claim has gone to the architect/client — records it as awaiting their approval (nothing is emailed)"),
            Item("Issue without approval", () => IssueAsync(invoice), "Skip the approval loop — counts toward certified to date"),
        },
        ValuationInvoiceStatus.Submitted => new[]
        {
            Item("Record approval", () => ApproveAsync(invoice), "Record the client's approval — issue next to count toward certified to date"),
            Item("Record rejection…", () => OpenReject(invoice), "Record the client's rejection — the invoice unlocks for amendment or cancellation"),
            Item("Issue without approval", () => IssueAsync(invoice), "For clients with no formal approval loop — counts toward certified to date"),
        },
        ValuationInvoiceStatus.Approved => new[]
        {
            Item("Issue invoice", () => IssueAsync(invoice), "Counts toward certified to date"),
        },
        ValuationInvoiceStatus.Issued => new[]
        {
            Item("Record payment…", () => OpenPayment(invoice), "Record the client's payment when the cash lands"),
        },
        _ => Array.Empty<DropdownMenu.Item>(),
    };

    private IEnumerable<DropdownMenu.Item> AmendItems(ValuationInvoice invoice)
    {
        if (invoice.IsEditable && invoice.Status != ValuationInvoiceStatus.Cancelled)
            yield return invoice.Status == ValuationInvoiceStatus.Rejected
                ? Item("Amend…", () => OpenEdit(invoice), "Amend and return to draft, ready to claim again", group: 1)
                : Item("Edit…", () => OpenEdit(invoice), "Amend period/amount", group: 1);
        if (!invoice.IsManual && invoice.Status is ValuationInvoiceStatus.Raised or ValuationInvoiceStatus.Rejected)
            yield return Item("Cancel invoice", () => CancelAsync(invoice),
                "Withdraw — kept for the audit trail, excluded from every total", group: 1, destructive: true);
    }

    private IEnumerable<DropdownMenu.Item> RecordItems(ValuationInvoice invoice)
    {
        var historyOpen = historyOpenId == invoice.ValuationInvoiceId;
        yield return Item(historyOpen ? "Hide history" : "History", () => ToggleHistoryAsync(invoice), group: 2);
        var countsTowardCertified = invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid;
        yield return Item("Delete…", () => pendingDeleteId = invoice.ValuationInvoiceId,
            countsTowardCertified
                ? "Delete this invoice — certified to date (and a paid one's receipts) roll back"
                : "Delete this invoice",
            group: 2, destructive: true);
    }

    private DropdownMenu.Item Item(string label, Func<Task> onSelect, string? hint = null, int group = 0, bool destructive = false) =>
        new(Label: label, OnSelect: EventCallback.Factory.Create(this, onSelect), Hint: hint, Destructive: destructive, Group: group);

    private DropdownMenu.Item Item(string label, Action onSelect, string? hint = null, int group = 0, bool destructive = false) =>
        new(Label: label, OnSelect: EventCallback.Factory.Create(this, onSelect), Hint: hint, Destructive: destructive, Group: group);
}
