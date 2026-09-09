namespace Jewel.JPMS.Features.Commercial;

/// <summary>How a valuation invoice's status and history read on screen — the badge, its
/// hover text and the audit trail's event names.</summary>
public static class ValuationInvoiceDisplay
{
    public static string StatusLabel(ValuationInvoiceStatus status) => status switch
    {
        ValuationInvoiceStatus.Raised => "Draft",
        ValuationInvoiceStatus.Submitted => "Awaiting approval",
        ValuationInvoiceStatus.Approved => "Approved",
        ValuationInvoiceStatus.Rejected => "Rejected",
        ValuationInvoiceStatus.Issued => "Issued",
        ValuationInvoiceStatus.Paid => "Paid",
        ValuationInvoiceStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string StatusTitle(ValuationInvoice invoice) => invoice.Status switch
    {
        ValuationInvoiceStatus.Raised => "Draft — raised, not yet claimed: record the claim as sent, or issue directly",
        ValuationInvoiceStatus.Submitted => $"Claimed — with the architect/client for approval{(invoice.SubmittedAt is { } s ? $" since {s:dd MMM yyyy}" : "")}",
        ValuationInvoiceStatus.Approved => "Approved by the client — issue to count toward certified to date",
        ValuationInvoiceStatus.Rejected => "Rejected — amend and resubmit, or cancel",
        ValuationInvoiceStatus.Issued => "Issued to the client — counts toward certified to date",
        ValuationInvoiceStatus.Paid => "Paid — rolled into the project's paid total",
        ValuationInvoiceStatus.Cancelled => "Withdrawn — excluded from every total",
        _ => ""
    };


    public static string EventLabel(ValuationInvoiceEventType type) => type switch
    {
        ValuationInvoiceEventType.Created => "Created",
        ValuationInvoiceEventType.Submitted => "Submitted for approval",
        ValuationInvoiceEventType.Approved => "Approved",
        ValuationInvoiceEventType.Rejected => "Rejected",
        ValuationInvoiceEventType.Amended => "Amended",
        ValuationInvoiceEventType.Issued => "Issued",
        ValuationInvoiceEventType.PaymentRecorded => "Payment recorded",
        ValuationInvoiceEventType.Cancelled => "Cancelled",
        ValuationInvoiceEventType.ManualEntry => "Historic entry",
        ValuationInvoiceEventType.RaisedInXero => "Raised in Xero",
        _ => type.ToString()
    };
}
