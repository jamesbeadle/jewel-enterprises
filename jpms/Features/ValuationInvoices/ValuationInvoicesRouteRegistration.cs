using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Features.ValuationInvoices;

public static class ValuationInvoicesRouteRegistration
{
    public static void RegisterValuationInvoicesRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>>(
            new QueryRoute("/api/projects/{projectId}/valuation-invoices",
                query => $"/api/projects/{((ListValuationInvoicesForProject)query).ProjectId}/valuation-invoices"));

        queries.Register<GetProjectValuationInvoiceSummary, ProjectValuationInvoiceSummary>(
            new QueryRoute("/api/projects/{projectId}/valuation-invoices/summary",
                query => $"/api/projects/{((GetProjectValuationInvoiceSummary)query).ProjectId}/valuation-invoices/summary"));

        queries.Register<ListValuationInvoiceEvents, IReadOnlyList<ValuationInvoiceEvent>>(
            new QueryRoute("/api/valuation-invoices/{valuationInvoiceId}/events",
                query => $"/api/valuation-invoices/{((ListValuationInvoiceEvents)query).ValuationInvoiceId}/events"));

        commands.Register<CreateValuationInvoice, ValuationInvoice>(
            new CommandRoute("POST", "/api/projects/{projectId}/valuation-invoices",
                command => $"/api/projects/{((CreateValuationInvoice)command).ProjectId}/valuation-invoices"));

        commands.Register<UpdateValuationInvoice, ValuationInvoice>(
            new CommandRoute("PUT", "/api/valuation-invoices/{valuationInvoiceId}",
                command => $"/api/valuation-invoices/{((UpdateValuationInvoice)command).ValuationInvoiceId}"));

        commands.Register<SubmitValuationInvoice, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/submit",
                command => $"/api/valuation-invoices/{((SubmitValuationInvoice)command).ValuationInvoiceId}/submit"));

        commands.Register<ApproveValuationInvoice, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/approve",
                command => $"/api/valuation-invoices/{((ApproveValuationInvoice)command).ValuationInvoiceId}/approve"));

        commands.Register<RejectValuationInvoice, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/reject",
                command => $"/api/valuation-invoices/{((RejectValuationInvoice)command).ValuationInvoiceId}/reject"));

        commands.Register<CancelValuationInvoice, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/cancel",
                command => $"/api/valuation-invoices/{((CancelValuationInvoice)command).ValuationInvoiceId}/cancel"));

        commands.Register<IssueValuationInvoice, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/issue",
                command => $"/api/valuation-invoices/{((IssueValuationInvoice)command).ValuationInvoiceId}/issue"));

        queries.Register<PreviewValuationInvoiceXeroRaise, ValuationInvoiceXeroRaisePreview>(
            new QueryRoute("/api/valuation-invoices/{valuationInvoiceId}/xero-raise",
                query => XeroRaisePreviewUrl((PreviewValuationInvoiceXeroRaise)query)));

        commands.Register<RecordValuationInvoiceXeroNumber, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/xero-number",
                command => $"/api/valuation-invoices/{((RecordValuationInvoiceXeroNumber)command).ValuationInvoiceId}/xero-number"));

        commands.Register<RaiseValuationInvoiceInXero, ValuationInvoiceXeroRaiseOutcome>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/xero-raise",
                command => $"/api/valuation-invoices/{((RaiseValuationInvoiceInXero)command).ValuationInvoiceId}/xero-raise"));

        commands.Register<RecordValuationInvoicePayment, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/payment",
                command => $"/api/valuation-invoices/{((RecordValuationInvoicePayment)command).ValuationInvoiceId}/payment"));

        commands.Register<DeleteValuationInvoice, Acknowledgement>(
            new CommandRoute("DELETE", "/api/valuation-invoices/{valuationInvoiceId}",
                command => $"/api/valuation-invoices/{((DeleteValuationInvoice)command).ValuationInvoiceId}"));
    }

    // The user's dates ride the query string (yyyy-MM-dd); blank means today / the certificate rule.
    private static string XeroRaisePreviewUrl(PreviewValuationInvoiceXeroRaise query)
    {
        var parameters = new List<string>();
        if (query.InvoiceDate is { } invoiceDate) parameters.Add($"invoiceDate={invoiceDate:yyyy-MM-dd}");
        if (query.DueDate is { } dueDate) parameters.Add($"dueDate={dueDate:yyyy-MM-dd}");
        var url = $"/api/valuation-invoices/{query.ValuationInvoiceId}/xero-raise";
        return parameters.Count == 0 ? url : $"{url}?{string.Join("&", parameters)}";
    }
}
