using Jewel.JPMS.Api.Features.Boq.Commands;
using Jewel.JPMS.Api.Features.Lads;
using Jewel.JPMS.Api.Features.Lads.Commands;
using Jewel.JPMS.Api.Features.Retention.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class VariationsAndValuationsActions
{
    private static IEnumerable<AiAction> ValuationInvoicesActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_valuation_invoice",
            Area: "Valuation invoices",
            Description: "RAISES a monthly valuation invoice against a project — a real financial "
                + "record, created in the Raised state, drawn against a locked (Preapproved) "
                + "valuation claim that has no live invoice yet; the raise freezes the report "
                + "snapshot that becomes the client-facing statement. With isManual it records a "
                + "backdated historical invoice directly as Issued or Paid, counting fully toward "
                + "Certified to date and Total Paid.",
            CommandType: typeof(CreateValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(CreateValuationInvoiceAuthorisation),
            ValidationType: typeof(CreateValuationInvoiceValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm amount and period with the user before calling. projectId comes from "
                + "list_projects; valuationClaimId is required for a normal raise and must name a "
                + "Preapproved claim with no live invoice (get_valuation_context lists the claims "
                + "and their status) — a Draft claim or one already invoiced is refused with the "
                + "reason. amountPaid/issuedAt/paidAt apply to manual invoices only. The ladder for "
                + "normal invoices is Raised → Submitted → Approved → Issued → Paid."),

        new AiAction(
            Name: "update_valuation_invoice",
            Area: "Valuation invoices",
            Description: "AMENDS a valuation invoice's period and amount — a real financial change. "
                + "Amending a Rejected invoice returns it to Raised, ready to resubmit. For manual "
                + "invoices it may also revise the paid amount and backdated dates, recomputing "
                + "certified/paid totals in the same operation.",
            CommandType: typeof(UpdateValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: typeof(UpdateValuationInvoiceValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Allowed while Raised or Rejected — and at any status for a manual invoice. "
                + "Confirm the new figures with the user before calling."),

        new AiAction(
            Name: "submit_valuation_invoice",
            Area: "Valuation invoices",
            Description: "SUBMITS a Raised valuation invoice to the architect/client for approval "
                + "(Raised → Submitted) — a real state action that freezes a full valuation-report "
                + "snapshot and locks amount and period until the invoice is approved, rejected or "
                + "amended. It changes portal state; it does not itself send an email.",
            CommandType: typeof(SubmitValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — the frozen snapshot is what the client "
                + "will be answering to. The invoice must be Raised."),

        new AiAction(
            Name: "approve_valuation_invoice",
            Area: "Valuation invoices",
            Description: "RECORDS the client's approval of a Submitted valuation invoice "
                + "(Submitted → Approved) — a real financial/state action; only record it when the "
                + "client's approval actually exists. The amount still does not count toward "
                + "Certified to date until the invoice is issued.",
            CommandType: typeof(ApproveValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. The optional note captures the "
                + "architect's certificate reference/date. The invoice must be Submitted."),

        new AiAction(
            Name: "reject_valuation_invoice",
            Area: "Valuation invoices",
            Description: "RECORDS the client's rejection of a Submitted valuation invoice "
                + "(Submitted → Rejected) — a real state action. The required reason drives the "
                + "amendment; a Rejected invoice is unlocked to amend (back to Raised) or cancel.",
            CommandType: typeof(RejectValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: typeof(RejectValuationInvoiceValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling; only record a rejection the client has "
                + "actually made. reason is required."),

        new AiAction(
            Name: "raise_valuation_invoice_in_xero",
            Area: "Valuation invoices",
            Description: "WRITES TO XERO: raises the AUTHORISED sales invoice for a valuation invoice "
                + "on the project's client — one line for the invoice's net on the sales account "
                + "with the project's Sites tracking, VAT per Xero's own reading of the contact "
                + "(never assumed), the payment certificate PDF attached when the register holds "
                + "one — stamps Xero's invoice id and number on the valuation invoice, then ISSUES "
                + "it (Approved → Issued, or Raised/Submitted → Issued on the skip path): certified "
                + "to date moves. The certificate attachment is best effort — the invoice stands "
                + "without it and the outcome says so (attachmentError). Refused when the invoice is "
                + "already raised in Xero, is Rejected/Cancelled/Issued/Paid, or when the project has "
                + "no Xero site mapping. An invoice in Xero cannot be un-raised from here — void it "
                + "in Xero if it was wrong.",
            CommandType: typeof(RaiseValuationInvoiceInXero),
            ResultType: typeof(ValuationInvoiceXeroRaiseOutcome),
            AuthorisationType: typeof(RaiseValuationInvoiceInXeroAuthorisation),
            ValidationType: typeof(RaiseValuationInvoiceInXeroValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: new[] { nameof(RaiseValuationInvoiceInXero.RaisedBy) },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Call preview_valuation_invoice_xero_raise first and show the user everything it "
                + "returns — client, net, VAT reading, Sites option, due date, the certificate to be "
                + "attached — and its blockers; raise only when canRaise is true and the user has said "
                + "yes. Never invoice off Jewel's own valuation figure when a certificate says "
                + "otherwise: the valuation invoice's amount must already be the certified figure "
                + "(update_valuation_invoice fixes it first). issue_valuation_invoice is the route for "
                + "an invoice someone raised in Xero by hand."),

        new AiAction(
            Name: "issue_valuation_invoice",
            Area: "Valuation invoices",
            Description: "ISSUES a valuation invoice WITHOUT raising it in Xero — marks the client "
                + "invoice as sent (Approved → Issued, or Raised → Issued for projects that skip the "
                + "approval loop) for an invoice someone raised in Xero by hand. A real financial "
                + "action: from this point the amount counts toward Certified to date. The skip path "
                + "freezes a report snapshot if none is linked. To raise it in Xero from here as well, "
                + "use raise_valuation_invoice_in_xero instead.",
            CommandType: typeof(IssueValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(IssueValuationInvoiceAuthorisation),
            ValidationType: typeof(IssueValuationInvoiceValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — certified totals move."),

        new AiAction(
            Name: "record_valuation_invoice_payment",
            Area: "Valuation invoices",
            Description: "RECORDS the client's payment against a valuation invoice (→ Paid) — a "
                + "real financial record that increases the project's paid total by the amount "
                + "received. Money does not move; this records that it has.",
            CommandType: typeof(RecordValuationInvoicePayment),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(RecordValuationInvoicePaymentAuthorisation),
            ValidationType: typeof(RecordValuationInvoicePaymentValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the amount received with the user before calling."),

        new AiAction(
            Name: "cancel_valuation_invoice",
            Area: "Valuation invoices",
            Description: "WITHDRAWS a Raised or Rejected valuation invoice (→ Cancelled) — kept for "
                + "the audit trail but excluded from every total; its snapshots are flagged "
                + "superseded.",
            CommandType: typeof(CancelValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. Use delete_valuation_invoice to remove "
                + "an invoice entirely."),

        new AiAction(
            Name: "delete_valuation_invoice",
            Area: "Valuation invoices",
            Description: "DELETES a valuation invoice permanently — a real financial action: "
                + "removing an Issued/Paid invoice reduces Certified to date (re-freezing any "
                + "Preapproved claim's totals), and deleting a Paid one rolls its receipt out of "
                + "the project's paid total. There is no undo.",
            CommandType: typeof(DeleteValuationInvoice),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteValuationInvoiceAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user, naming the exact invoice and period, before calling."),

        // ── BoQ ───────────────────────────────────────────────────────────────────────────────

    };
}
