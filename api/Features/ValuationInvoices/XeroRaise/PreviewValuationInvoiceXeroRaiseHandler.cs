using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>The preview is the plan, shown — nothing is written.</summary>
public sealed class PreviewValuationInvoiceXeroRaiseHandler : IQueryHandler<PreviewValuationInvoiceXeroRaise, ValuationInvoiceXeroRaisePreview>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;
    private readonly XeroOptions options;

    public PreviewValuationInvoiceXeroRaiseHandler(JpmsContext context, IXeroClient xero, XeroOptions options)
    {
        this.context = context;
        this.xero = xero;
        this.options = options;
    }

    public async Task<ValuationInvoiceXeroRaisePreview> HandleAsync(PreviewValuationInvoiceXeroRaise query, CancellationToken cancellationToken) =>
        (await new ValuationInvoiceXeroRaisePlanner(context, xero, options).PlanAsync(query.ValuationInvoiceId, query.InvoiceDate, query.DueDate, cancellationToken)).ToPreview();
}
