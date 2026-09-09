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
            "What raise_valuation_invoice_in_xero WOULD create for one valuation invoice: the "
            + "client as Xero knows them (matched, or created with the invoice), the net amount, "
            + "the line description, the sales account, the project's Sites tracking option, the "
            + "invoice and due dates, where the VAT treatment comes from (the contact's default, "
            + "their last sales invoice, or Xero's account default — never assumed), and the "
            + "payment certificate that will be attached. blockers lists every reason it would be "
            + "refused (already raised, wrong status, no Xero site mapping); canRaise is true only "
            + "when that list is empty. Reads Xero fresh. Call this before the raise and show the "
            + "user all of it.",
            AiToolSchema.Object(
                ("valuationInvoiceId", "string", "The valuation invoice (from list_valuation_invoices).", true)),
            AiToolKind.Read,
            ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            async (context, input, ct) =>
            {
                var valuationInvoiceId = AiToolSchema.Text(input, "valuationInvoiceId");
                if (string.IsNullOrWhiteSpace(valuationInvoiceId)) return Fail("valuationInvoiceId is required.");
                try
                {
                    var preview = await context.Services
                        .GetRequiredService<IQueryHandler<PreviewValuationInvoiceXeroRaise, ValuationInvoiceXeroRaisePreview>>()
                        .HandleAsync(new PreviewValuationInvoiceXeroRaise(valuationInvoiceId), ct);
                    return Serialise(new
                    {
                        ok = true,
                        preview,
                        canRaise = preview.CanRaise,
                        note = preview.CanRaise
                            ? "Raising creates an AUTHORISED sales invoice in Xero and issues the valuation invoice (certified to date moves). Take the user's yes, then raise_valuation_invoice_in_xero."
                            : "Blocked — resolve the blockers first, or issue_valuation_invoice if the invoice was raised in Xero by hand."
                    });
                }
                catch (InvalidOperationException refusal)
                {
                    return Fail(refusal.Message);
                }
            })
    };
}
