using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;
using Jewel.JPMS.Api.Features.Xero.Queries;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Xero;

/// <summary>
/// Registers the Xero ledger read used for financial reconciliation. Real client when a Xero custom
/// connection's client id/secret are present in configuration, otherwise a no-op so the rest of the
/// app runs unchanged and the ledger UI explains that the credentials are missing. The credentials
/// are read from app settings / Key Vault only (Xero__ClientId, Xero__ClientSecret) — never from
/// source control.
/// </summary>
public static class XeroFeatureRegistration
{
    public static IServiceCollection AddXeroFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = XeroOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instance so it doesn't clash with the Graph client's registration.
            // Singleton so the cached access token is shared across requests. Automatic
            // decompression matters: some Xero responses arrive gzipped, and without it the
            // body reads as byte garbage — which is what the ExtractXeroErrors path would
            // then relay to the user's error toast.
            services.AddSingleton<IXeroClient>(sp =>
                new XeroClient(
                    new HttpClient(new HttpClientHandler
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.All
                    })
                    {
                        // Explicit, and well under the Static Web Apps gateway's ~45s: the default
                        // is 100s, so a Xero call that stopped answering would hold a request open
                        // long past the point the gateway had already given the user a 504. Fail
                        // fast instead and let the snapshot report the failure. Paged reads make
                        // several calls, so this is per call, not per page-through.
                        Timeout = TimeSpan.FromSeconds(20)
                    },
                    options,
                    sp.GetRequiredService<ILogger<XeroClient>>()));
        }
        else
        {
            services.AddSingleton<IXeroClient, NullXeroClient>();
        }

        services.AddScoped<IQueryHandler<ListXeroTransactions, XeroTransactionsSnapshot>, ListXeroTransactionsHandler>();

        // Cash summary: bank balances + outstanding sales invoices for the company Cash Summary page.
        services.AddScoped<IQueryHandler<GetXeroCashSummary, XeroCashSummarySnapshot>, GetXeroCashSummaryHandler>();

        // Aged payables: outstanding supplier bills aged like Xero's report, drafts included —
        // the report Xero itself cannot show while bills wait in draft for portal coding.
        services.AddScoped<IQueryHandler<GetXeroAgedPayables, XeroAgedPayablesSnapshot>, GetXeroAgedPayablesHandler>();

        // Aged receivables: the sales-side mirror — outstanding client invoices aged like
        // Xero's report, drafts included.
        services.AddScoped<IQueryHandler<GetXeroAgedReceivables, XeroAgedReceivablesSnapshot>, GetXeroAgedReceivablesHandler>();

        // Suppliers: the contact list behind the directory's "Import from Xero" modal.
        services.AddScoped<IQueryHandler<ListXeroSuppliers, XeroSuppliersSnapshot>, ListXeroSuppliersHandler>();

        // Tracking categories: Xero's Sites / Cost Code options verbatim, for the Cost codes
        // page's Xero tabs — reading the exact phrasing when linking projects and codes up.
        services.AddScoped<IQueryHandler<ListXeroTrackingCategories, XeroTrackingCategoriesSnapshot>,
            ListXeroTrackingCategoriesHandler>();

        // Ledger allocation: stored Xero lines reconciled onto projects + master cost centres.
        services.AddScoped<ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult>, SyncXeroLedgerHandler>();
        services.AddScoped<IQueryHandler<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>, ListXeroLedgerLinesHandler>();
        services.AddScoped<IQueryHandler<GetXeroLedgerCounts, XeroLedgerCounts>, GetXeroLedgerCountsHandler>();
        services.AddScoped<IQueryHandler<ListXeroLedgerLinesForProject, IReadOnlyList<XeroLedgerLine>>,
            ListXeroLedgerLinesForProjectHandler>();
        services.AddScoped<ICommandHandler<SetXeroAllocation, int>, SetXeroAllocationHandler>();
        // The gate + shape check as classes, for the connector's set_xero_allocation action
        // (2026-09-03 — the accountant could read the queue but not press Allocate). The HTTP
        // endpoint keeps its inline role check; both read XeroLedgerRoles.AllowedToAllocate.
        services.AddScoped<SetXeroAllocationAuthorisation>();
        services.AddScoped<SetXeroAllocationValidation>();
        services.AddScoped<ICommandHandler<AllocateSuggestedXeroLines, int>, AllocateSuggestedXeroLinesHandler>();
        services.AddScoped<IQueryHandler<ListXeroInvoiceAttachments, IReadOnlyList<XeroInvoiceAttachment>>,
            ListXeroInvoiceAttachmentsHandler>();

        // Write-back: once a draft bill's lines are all allocated, its Sites/Cost Code
        // tracking is confirmed onto the Xero invoice and the invoice is approved.
        services.AddScoped<IXeroWriteBackService, XeroWriteBackService>();
        services.AddScoped<ICommandHandler<RetryXeroWriteBack, XeroWriteBackOutcome>, RetryXeroWriteBackHandler>();

        // Work Order bills (2026-09-08): the FD's one-press approval of a bill matched to an open
        // work order, and its undo. Gates as classes for the endpoint and the connector alike.
        services.AddScoped<ICommandHandler<ApproveWorkOrderBill, WorkOrderBillApprovalOutcome>, ApproveWorkOrderBillHandler>();
        services.AddScoped<ApproveWorkOrderBillAuthorisation>();
        services.AddScoped<ApproveWorkOrderBillValidation>();
        services.AddScoped<ICommandHandler<UndoWorkOrderBillApproval, WorkOrderBillUndoOutcome>, UndoWorkOrderBillApprovalHandler>();
        services.AddScoped<UndoWorkOrderBillApprovalAuthorisation>();
        services.AddScoped<UndoWorkOrderBillApprovalValidation>();

        // Site P&L: the stored monthly income/cost per project from Xero's P&L report filtered
        // by Sites tracking — the Profit Summary's cumulative chart. Synced nightly + on demand.
        services.AddScoped<IQueryHandler<GetXeroSitePnl, XeroSitePnlSnapshot>, SitePnl.GetXeroSitePnlHandler>();
        services.AddScoped<ICommandHandler<SyncXeroSitePnl, XeroSitePnlSyncResult>, SitePnl.SyncXeroSitePnlHandler>();

        // Cost Code tracking options vs the portal's master (2026-09-03): the gap read only — the
        // portal is deliberately NOT granted Xero's settings write scope, so options are created
        // by hand in Xero from this list (Cost codes page + connector).
        services.AddScoped<IQueryHandler<GetXeroCostCodeOptionGaps, XeroCostCodeOptionGaps>,
            TrackingOptions.GetXeroCostCodeOptionGapsHandler>();

        return services;
    }
}
