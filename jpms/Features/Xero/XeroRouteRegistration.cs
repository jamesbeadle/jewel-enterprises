
namespace Jewel.JPMS.Features.Xero;

public static class XeroRouteRegistration
{
    public static IServiceCollection AddXeroReadModels(this IServiceCollection services)
    {
        services.AddScoped<XeroTransactionsReadModel>();
        services.AddScoped<XeroCashSummaryReadModel>();
        services.AddScoped<XeroAgedPayablesReadModel>();
        services.AddScoped<XeroAgedReceivablesReadModel>();
        services.AddScoped<XeroLedgerReadModel>();
        services.AddScoped<XeroSitePnlReadModel>();
        services.AddScoped<XeroTrackingCategoriesReadModel>();
        return services;
    }

    public static void RegisterXeroRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListXeroTransactions, XeroTransactionsSnapshot>(
            new QueryRoute("/api/xero/transactions",
                query => ((ListXeroTransactions)query).Force ? "/api/xero/transactions?force=true" : "/api/xero/transactions"));

        queries.Register<ListXeroSuppliers, XeroSuppliersSnapshot>(
            new QueryRoute("/api/xero/suppliers",
                query => ((ListXeroSuppliers)query).Force ? "/api/xero/suppliers?force=true" : "/api/xero/suppliers"));

        queries.Register<ListXeroTrackingCategories, XeroTrackingCategoriesSnapshot>(
            new QueryRoute("/api/xero/tracking-categories",
                query => ((ListXeroTrackingCategories)query).Force ? "/api/xero/tracking-categories?force=true" : "/api/xero/tracking-categories"));

        queries.Register<GetXeroCashSummary, XeroCashSummarySnapshot>(
            new QueryRoute("/api/xero/cash-summary",
                query => ((GetXeroCashSummary)query).Force ? "/api/xero/cash-summary?force=true" : "/api/xero/cash-summary"));

        queries.Register<GetXeroAgedPayables, XeroAgedPayablesSnapshot>(
            new QueryRoute("/api/xero/aged-payables",
                query => ((GetXeroAgedPayables)query).Force ? "/api/xero/aged-payables?force=true" : "/api/xero/aged-payables"));

        queries.Register<GetXeroAgedReceivables, XeroAgedReceivablesSnapshot>(
            new QueryRoute("/api/xero/aged-receivables",
                query => ((GetXeroAgedReceivables)query).Force ? "/api/xero/aged-receivables?force=true" : "/api/xero/aged-receivables"));

        // The allocation page reads one status at a time; ?status= is what keeps it from
        // downloading the whole ledger to render one tab.
        queries.Register<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>(
            new QueryRoute("/api/xero/ledger", query =>
            {
                var status = ((ListXeroLedgerLines)query).Status;
                return status is null ? "/api/xero/ledger" : $"/api/xero/ledger?status={status}";
            }));

        queries.Register<GetXeroLedgerCounts, XeroLedgerCounts>(QueryRoute.Static("/api/xero/ledger/counts"));

        queries.Register<ListXeroLedgerLinesForProject, IReadOnlyList<XeroLedgerLine>>(
            new QueryRoute("/api/projects/{projectId}/xero/ledger", query =>
            {
                var forProject = (ListXeroLedgerLinesForProject)query;
                return $"/api/projects/{forProject.ProjectId}/xero/ledger?take={forProject.Take}";
            }));

        queries.Register<ListXeroInvoiceAttachments, IReadOnlyList<XeroInvoiceAttachment>>(
            new QueryRoute("/api/xero/invoice/attachments", query =>
            {
                var attachments = (ListXeroInvoiceAttachments)query;
                return $"/api/xero/invoice/attachments?id={Uri.EscapeDataString(attachments.XeroInvoiceId)}"
                       + (attachments.IsCreditNote ? "&credit=1" : "");
            }));

        // Site P&L: the stored monthly income/cost per project behind the Profit Summary's
        // cumulative chart, plus the explicit re-pull from Xero.
        queries.Register<GetXeroSitePnl, XeroSitePnlSnapshot>(QueryRoute.Static("/api/xero/site-pnl"));

        commands.Register<SyncXeroLedger, XeroLedgerSyncResult>(CommandRoute.Post("/api/xero/ledger/sync"));
        commands.Register<SyncXeroSitePnl, XeroSitePnlSyncResult>(CommandRoute.Post("/api/xero/site-pnl/sync"));

        // Cost Code tracking options vs the master (2026-09-03): the gap read behind the Cost
        // codes page's "List for Xero" — options are created by hand in Xero, never by the portal.
        queries.Register<GetXeroCostCodeOptionGaps, XeroCostCodeOptionGaps>(QueryRoute.Static("/api/xero/tracking-options/cost-codes/gaps"));
        commands.Register<SetXeroAllocation, int>(CommandRoute.Post("/api/xero/allocations"));
        commands.Register<AllocateSuggestedXeroLines, int>(CommandRoute.Post("/api/xero/allocations/suggested"));
        commands.Register<RetryXeroWriteBack, XeroWriteBackOutcome>(CommandRoute.Post("/api/xero/writeback/retry"));
        // Work Order bills (2026-09-08): the FD's one-press approval and its undo.
        commands.Register<ApproveWorkOrderBill, WorkOrderBillApprovalOutcome>(CommandRoute.Post("/api/xero/work-order-bills/approve"));
        commands.Register<UndoWorkOrderBillApproval, WorkOrderBillUndoOutcome>(CommandRoute.Post("/api/xero/work-order-bills/undo"));
    }
}
