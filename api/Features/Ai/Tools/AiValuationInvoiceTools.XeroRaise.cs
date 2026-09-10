using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The claim card's "Raise in Xero" preview as a connector read (2026-09-09, the accountant's
/// ask): exactly what raise_valuation_invoice_in_xero would put in Xero — client, amount, VAT
/// reading, Sites option, due date, certificate to attach — and every reason it would be refused.
/// </summary>
internal static partial class AiValuationInvoiceTools
{
    private static IEnumerable<AiTool> XeroRaiseTools() => new AiTool[]
    {
        new(
            "preview_valuation_invoice_xero_raise",
            "What raise_valuation_invoice_in_xero WOULD create for one valuation invoice: the Xero "
            + "contact MAPPED ON THE PROJECT and whether Xero still holds it (contactName, "
            + "xeroContactId, contactStatus — the raise never matches by name and never creates a "
            + "contact), the net amount, Xero's reference (\"Valuation NN\") and the line description, "
            + "the sales account, the project's Sites tracking option, the invoice and due dates "
            + "(pass invoiceDate / dueDate, yyyy-MM-dd, to use the user's; blank means today and the "
            + "certificate's issue date + the contract's payment days, else Xero's default — "
            + "dueDateNote says which applied), where the VAT treatment comes from (the contact's "
            + "default, their last sales invoice, or Xero's account default — never assumed), and "
            + "the payment certificate that will be attached. blockers lists every reason it would "
            + "be refused (already raised — by the portal or a hand-recorded number — wrong status, "
            + "no Xero contact mapped on the project, mapped contact not found in Xero, no Xero site "
            + "mapping); canRaise is true only when that list is empty. Reads Xero fresh. Call this "
            + "before the raise and show the user all of it. When a blocker says no Xero contact is "
            + "mapped, STOP: do not raise — the user sets the Xero contact in Project settings, or "
            + "you do it with update_project_details (xeroContactId + xeroContactName from the Xero "
            + "contacts list in Project settings) once they say yes — then preview again.",
            AiToolSchema.Object(
                ("valuationInvoiceId", "string", "The valuation invoice (from list_valuation_invoices).", true),
                ("invoiceDate", "string", "The invoice date to raise with, yyyy-MM-dd. Blank = today.", false),
                ("dueDate", "string", "The due date to raise with, yyyy-MM-dd. Blank = the certificate's issue date + the contract's final date for payment days, else Xero's sales default.", false)),
            AiToolKind.Read,
            ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            async (context, input, ct) =>
            {
                var valuationInvoiceId = AiToolSchema.Text(input, "valuationInvoiceId");
                if (string.IsNullOrWhiteSpace(valuationInvoiceId)) return Fail("valuationInvoiceId is required.");
                if (!TryDate(AiToolSchema.Text(input, "invoiceDate"), out var invoiceDate)) return Fail("invoiceDate must be a date, yyyy-MM-dd.");
                if (!TryDate(AiToolSchema.Text(input, "dueDate"), out var dueDate)) return Fail("dueDate must be a date, yyyy-MM-dd.");
                try
                {
                    var preview = await context.Services
                        .GetRequiredService<IQueryHandler<PreviewValuationInvoiceXeroRaise, ValuationInvoiceXeroRaisePreview>>()
                        .HandleAsync(new PreviewValuationInvoiceXeroRaise(valuationInvoiceId, invoiceDate, dueDate), ct);
                    var contactBlocked = preview.Blockers.Any(blocker => blocker.Contains("Xero contact", StringComparison.OrdinalIgnoreCase));
                    return Serialise(new
                    {
                        ok = true,
                        preview,
                        canRaise = preview.CanRaise,
                        note = preview.CanRaise
                            ? $"Raising creates an AUTHORISED sales invoice in Xero on {preview.ContactName} ({preview.ContactStatus}), dated {preview.Date:yyyy-MM-dd}"
                              + (preview.DueDate is { } due ? $", due {due:yyyy-MM-dd}" : ", due per Xero's default")
                              + ", and issues the valuation invoice (certified to date moves). Confirm the dates and the contact with the user, take their yes, then raise_valuation_invoice_in_xero with the same invoiceDate/dueDate."
                            : contactBlocked
                                ? "STOP — blocked on the project's Xero contact mapping. Do not raise and do not create a contact: the user sets the Xero contact in Project settings (or you set it with update_project_details xeroContactId + xeroContactName, with their yes), then preview again."
                                : "Blocked — resolve the blockers first, or issue_valuation_invoice (with xeroInvoiceNumber) if the invoice was raised in Xero by hand."
                    });
                }
                catch (InvalidOperationException refusal)
                {
                    return Fail(refusal.Message);
                }
            })
    };

    /// <summary>A blank is "not given" (null, true); anything else must parse as a date.</summary>
    private static bool TryDate(string? raw, out DateTime? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(raw)) return true;
        if (!DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsed))
            return false;
        date = parsed.Date;
        return true;
    }
}
