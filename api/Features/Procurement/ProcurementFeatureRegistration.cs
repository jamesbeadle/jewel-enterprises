using Jewel.JPMS.Api.Features.Procurement.Attachments;
using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Api.Features.Procurement.Queries;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Procurement;

public static class ProcurementFeatureRegistration
{
    public static IServiceCollection AddProcurementFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterAttachmentStore(services, configuration);
        RegisterBidPackageAttachmentStore(services, configuration);
        RegisterCompanyTenderTermsStore(services, configuration);

        // Attachments kept on a work order for record keeping (never sent to the supplier).
        services.AddScoped<IQueryHandler<ListWorkOrderAttachments, IReadOnlyList<WorkOrderAttachment>>,
            ListWorkOrderAttachmentsHandler>();
        services.AddScoped<ICommandHandler<RemoveWorkOrderAttachment, IReadOnlyList<WorkOrderAttachment>>,
            RemoveWorkOrderAttachmentHandler>();
        // Tender-document attachments on a bid package (supplier-facing — they travel with the
        // invite draft alongside the linked drawings).
        services.AddScoped<IQueryHandler<ListBidPackageAttachments, IReadOnlyList<BidPackageAttachment>>,
            ListBidPackageAttachmentsHandler>();
        services.AddScoped<ICommandHandler<RemoveBidPackageAttachment, IReadOnlyList<BidPackageAttachment>>,
            RemoveBidPackageAttachmentHandler>();

        services.AddScoped<IQueryHandler<ListBidPackagesForProject, IReadOnlyList<BidPackage>>, ListBidPackagesForProjectHandler>();
        services.AddScoped<IQueryHandler<GetBidPackageById, BidPackage?>, GetBidPackageByIdHandler>();
        services.AddScoped<IQueryHandler<ListQuotesForBidPackage, IReadOnlyList<Quote>>, ListQuotesForBidPackageHandler>();
        services.AddScoped<IQueryHandler<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>>, ListProjectWorkOrdersHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageRecipients, IReadOnlyList<BidPackageRecipient>>, ListBidPackageRecipientsHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>, ListBidPackageLineItemsHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageEmails, IReadOnlyList<MailboxMessage>>, ListBidPackageEmailsHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageEmailDispositions, IReadOnlyList<BidPackageEmailDisposition>>, ListBidPackageEmailDispositionsHandler>();
        services.AddScoped<IQueryHandler<ListQuoteLineItemsForBidPackage, IReadOnlyList<QuoteLineItem>>, ListQuoteLineItemsForBidPackageHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageDrawings, IReadOnlyList<Drawing>>, ListBidPackageDrawingsHandler>();
        services.AddScoped<IQueryHandler<SearchLocalSubcontractors, LocalSubcontractorSearchResult>, SearchLocalSubcontractorsHandler>();
        // Works out the search trade from the package's own title and details (one cheap AI call),
        // and carries the readiness gate on inviting subcontractors at all.
        services.AddScoped<IQueryHandler<ResolveBidPackageTrade, BidPackageTradeResolution>, ResolveBidPackageTradeHandler>();

        services.AddScoped<ICommandHandler<CreateBidPackage, BidPackage>, CreateBidPackageHandler>();
        services.AddScoped<CreateBidPackageAuthorisation>();
        services.AddScoped<CreateBidPackageValidation>();

        // AI proposals for what to tender next, read from the live valuation report. Read-only:
        // creating the chosen packages goes through CreateBidPackage like any other.
        services.AddScoped<ICommandHandler<SuggestBidPackages, BidPackageSuggestionResult>, SuggestBidPackagesHandler>();
        services.AddScoped<SuggestBidPackagesAuthorisation>();
        services.AddScoped<SuggestBidPackagesValidation>();

        services.AddScoped<ICommandHandler<CreateBidPackageFromMessage, BidPackage>, CreateBidPackageFromMessageHandler>();
        services.AddScoped<CreateBidPackageFromMessageAuthorisation>();
        services.AddScoped<CreateBidPackageFromMessageValidation>();

        services.AddScoped<ICommandHandler<InviteSubcontractorsToBidPackage, IReadOnlyList<BidPackageRecipient>>, InviteSubcontractorsToBidPackageHandler>();
        services.AddScoped<InviteSubcontractorsToBidPackageAuthorisation>();
        services.AddScoped<InviteSubcontractorsToBidPackageValidation>();

        services.AddScoped<ICommandHandler<RemoveBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>, RemoveBidPackageRecipientHandler>();
        services.AddScoped<RemoveBidPackageRecipientAuthorisation>();
        services.AddScoped<RemoveBidPackageRecipientValidation>();

        services.AddScoped<ICommandHandler<DeclineBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>, DeclineBidPackageRecipientHandler>();
        services.AddScoped<DeclineBidPackageRecipientAuthorisation>();
        services.AddScoped<DeclineBidPackageRecipientValidation>();

        services.AddScoped<ICommandHandler<SetBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>, SetBidPackageLineItemsHandler>();
        services.AddScoped<SetBidPackageLineItemsAuthorisation>();
        services.AddScoped<SetBidPackageLineItemsValidation>();

        services.AddScoped<ICommandHandler<AddBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>, AddBidPackageLineItemsHandler>();
        services.AddScoped<AddBidPackageLineItemsAuthorisation>();
        services.AddScoped<AddBidPackageLineItemsValidation>();

        services.AddScoped<ICommandHandler<SetBidPackageLineItemCoverage, IReadOnlyList<BidPackageLineItem>>, SetBidPackageLineItemCoverageHandler>();
        services.AddScoped<SetBidPackageLineItemCoverageAuthorisation>();
        services.AddScoped<SetBidPackageLineItemCoverageValidation>();

        services.AddScoped<ICommandHandler<UpdateBidPackageScope, BidPackage>, UpdateBidPackageScopeHandler>();
        services.AddScoped<UpdateBidPackageScopeAuthorisation>();
        services.AddScoped<UpdateBidPackageScopeValidation>();

        // Deletes a package raised in error (or an unwanted AI suggestion) with everything under
        // it. The handler refuses Awarded packages and anything a work order references.
        services.AddScoped<ICommandHandler<DeleteBidPackage, Jewel.JPMS.Contracts.Cqrs.Acknowledgement>, DeleteBidPackageHandler>();
        services.AddScoped<DeleteBidPackageAuthorisation>();
        services.AddScoped<DeleteBidPackageValidation>();

        // Ends the tender without a winner / puts it back in play. No reason is captured — closing
        // without incident needs no paperwork.
        services.AddScoped<ICommandHandler<CloseBidPackage, BidPackage>, CloseBidPackageHandler>();
        services.AddScoped<CloseBidPackageAuthorisation>();
        services.AddScoped<CloseBidPackageValidation>();

        services.AddScoped<ICommandHandler<ReopenBidPackage, BidPackage>, ReopenBidPackageHandler>();
        services.AddScoped<ReopenBidPackageAuthorisation>();
        services.AddScoped<ReopenBidPackageValidation>();

        services.AddScoped<ICommandHandler<SetBidPackageDrawings, IReadOnlyList<Drawing>>, SetBidPackageDrawingsHandler>();
        services.AddScoped<SetBidPackageDrawingsAuthorisation>();
        services.AddScoped<SetBidPackageDrawingsValidation>();

        // One attachment plan for both invite paths — the Outlook-draft flow and the in-app send —
        // so the two can never disagree about what a tenderer receives.
        services.AddScoped<BidPackageInviteMailAssembler>();

        services.AddScoped<ICommandHandler<PrepareBidPackageInviteDraft, BidPackageInviteDraft>, PrepareBidPackageInviteDraftHandler>();
        services.AddScoped<PrepareBidPackageInviteDraftAuthorisation>();
        services.AddScoped<PrepareBidPackageInviteDraftValidation>();

        // The in-app invite composer (2026-08-16): the invite is composed, persisted as a draft ON
        // the package, and SENT from the projects mailbox — no trip to Outlook. Same review-then-
        // send discipline, just reviewed here; the send still passes through the system's single
        // Graph send chokepoint.
        services.AddScoped<ICommandHandler<SendBidPackageInvite, BidPackageInviteSendOutcome>, SendBidPackageInviteHandler>();
        services.AddScoped<IQueryHandler<GetBidPackageInviteComposerDraft, BidPackageInviteComposerDraft?>, GetBidPackageInviteComposerDraftHandler>();
        services.AddScoped<ICommandHandler<SaveBidPackageInviteComposerDraft, Jewel.JPMS.Contracts.Cqrs.Acknowledgement>, SaveBidPackageInviteComposerDraftHandler>();

        // Same review-then-send-from-Outlook convention as the invite draft above.
        services.AddScoped<ICommandHandler<PrepareWorkOrderEmailDraft, WorkOrderEmailDraft>, PrepareWorkOrderEmailDraftHandler>();
        services.AddScoped<PrepareWorkOrderEmailDraftAuthorisation>();
        services.AddScoped<PrepareWorkOrderEmailDraftValidation>();

        // The threaded variant (2026-08-29): a REPLY draft inside an existing conversation linked
        // to the order, carrying the rendered purchase-order PDF — so the formal PO lands in the
        // email chain the works were agreed in. Same review-then-send-from-Outlook convention;
        // never a status side effect.
        services.AddScoped<ICommandHandler<PrepareWorkOrderReplyDraft, WorkOrderReplyDraft>, PrepareWorkOrderReplyDraftHandler>();
        services.AddScoped<PrepareWorkOrderReplyDraftAuthorisation>();
        services.AddScoped<PrepareWorkOrderReplyDraftValidation>();

        // The automatic counterpart: SENDS the purchase-order email the moment an order is
        // released (created un-drafted, or a draft approved) — the UI warns before firing it.
        services.AddScoped<ICommandHandler<SendWorkOrderPoEmail, WorkOrderPoEmailOutcome>, SendWorkOrderPoEmailHandler>();
        services.AddScoped<SendWorkOrderPoEmailAuthorisation>();
        services.AddScoped<SendWorkOrderPoEmailValidation>();

        // Tender return leg: Claude reads a filed tender email (body + returned pricing-schedule
        // spreadsheet) into a reviewable proposal; filing marks the sender's recipient Responded.
        services.AddScoped<ICommandHandler<ExtractTenderFromMessage, TenderExtraction>, ExtractTenderFromMessageHandler>();
        services.AddScoped<ExtractTenderFromMessageAuthorisation>();
        services.AddScoped<ExtractTenderFromMessageValidation>();

        services.AddScoped<ICommandHandler<RecordTenderResponse, IReadOnlyList<BidPackageRecipient>>, RecordTenderResponseHandler>();
        services.AddScoped<RecordTenderResponseAuthorisation>();
        services.AddScoped<RecordTenderResponseValidation>();

        services.AddScoped<ICommandHandler<SaveExtractedQuote, Quote>, SaveExtractedQuoteHandler>();
        services.AddScoped<SaveExtractedQuoteAuthorisation>();
        services.AddScoped<SaveExtractedQuoteValidation>();
        services.AddScoped<ICommandHandler<SetBidPackageEmailDisposition, IReadOnlyList<BidPackageEmailDisposition>>, SetBidPackageEmailDispositionHandler>();
        services.AddScoped<SetBidPackageEmailDispositionAuthorisation>();
        services.AddScoped<SetBidPackageEmailDispositionValidation>();

        services.AddScoped<ICommandHandler<SubmitQuoteForBidPackage, Quote>, SubmitQuoteForBidPackageHandler>();
        services.AddScoped<SubmitQuoteForBidPackageAuthorisation>();
        services.AddScoped<SubmitQuoteForBidPackageValidation>();

        services.AddScoped<ICommandHandler<ReviseQuote, Quote>, ReviseQuoteHandler>();
        services.AddScoped<ReviseQuoteAuthorisation>();
        services.AddScoped<ReviseQuoteValidation>();

        services.AddScoped<ICommandHandler<AwardBidPackage, WorkOrder>, AwardBidPackageHandler>();
        services.AddScoped<AwardBidPackageAuthorisation>();
        services.AddScoped<AwardBidPackageValidation>();

        services.AddScoped<ICommandHandler<IssueWorkOrderForVariationOrder, WorkOrder>, IssueWorkOrderForVariationOrderHandler>();

        services.AddScoped<ICommandHandler<UpdateWorkOrder, WorkOrder>, UpdateWorkOrderHandler>();
        services.AddScoped<UpdateWorkOrderAuthorisation>();
        services.AddScoped<UpdateWorkOrderValidation>();

        services.AddScoped<ICommandHandler<RecodeWorkOrderLine, IReadOnlyList<WorkOrderLine>>, RecodeWorkOrderLineHandler>();
        services.AddScoped<RecodeWorkOrderLineAuthorisation>();
        services.AddScoped<RecodeWorkOrderLineValidation>();

        services.AddScoped<ICommandHandler<CreateManualWorkOrder, WorkOrder>, CreateManualWorkOrderHandler>();
        services.AddScoped<CreateManualWorkOrderAuthorisation>();
        services.AddScoped<CreateManualWorkOrderValidation>();

        // The Control Centre's "create new work order from this email" — the manual-order
        // handler wrapped with the email link, mirroring CreateBidPackageFromMessage.
        services.AddScoped<ICommandHandler<CreateWorkOrderFromMessage, WorkOrder>, CreateWorkOrderFromMessageHandler>();
        services.AddScoped<CreateWorkOrderFromMessageAuthorisation>();
        services.AddScoped<CreateWorkOrderFromMessageValidation>();

        services.AddScoped<ICommandHandler<UpdateManualWorkOrder, WorkOrder>, UpdateManualWorkOrderHandler>();
        services.AddScoped<UpdateManualWorkOrderAuthorisation>();
        services.AddScoped<UpdateManualWorkOrderValidation>();

        services.AddScoped<ICommandHandler<ApproveWorkOrder, WorkOrder>, ApproveWorkOrderHandler>();
        services.AddScoped<ApproveWorkOrderAuthorisation>();
        services.AddScoped<ApproveWorkOrderValidation>();

        services.AddScoped<ICommandHandler<RejectWorkOrder, WorkOrder>, RejectWorkOrderHandler>();
        services.AddScoped<RejectWorkOrderAuthorisation>();
        services.AddScoped<RejectWorkOrderValidation>();

        services.AddScoped<ICommandHandler<DeleteDraftWorkOrder, Jewel.JPMS.Contracts.Cqrs.Acknowledgement>, DeleteDraftWorkOrderHandler>();
        services.AddScoped<DeleteDraftWorkOrderAuthorisation>();
        services.AddScoped<DeleteDraftWorkOrderValidation>();

        services.AddScoped<ICommandHandler<CancelWorkOrder, WorkOrder>, CancelWorkOrderHandler>();
        services.AddScoped<CancelWorkOrderAuthorisation>();
        services.AddScoped<CancelWorkOrderValidation>();

        return services;
    }

    // Work-order attachments share the drawings storage account by default — one connection string
    // to configure, one backup story — but can be pointed at their own account if volume ever
    // warrants it. Same chain as the request attachment store.
    private static void RegisterAttachmentStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["WorkOrderAttachmentsStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IWorkOrderAttachmentStore, NullWorkOrderAttachmentStore>();
        else
            services.AddSingleton<IWorkOrderAttachmentStore>(
                _ => new AzureBlobWorkOrderAttachmentStore(connectionString));
    }

    // The company's standard tender Terms & Conditions PDF — one blob, company-wide, attached to
    // every invite. Same connection chain as the other document stores.
    private static void RegisterCompanyTenderTermsStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["CompanyDocumentsStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<Attachments.ICompanyTenderTermsStore, Attachments.NullCompanyTenderTermsStore>();
        else
            services.AddSingleton<Attachments.ICompanyTenderTermsStore>(
                _ => new Attachments.AzureBlobCompanyTenderTermsStore(connectionString));
    }

    // Bid-package attachments follow the same chain, with their own key first so they can be
    // split onto a different account if tender-document volume ever warrants it.
    private static void RegisterBidPackageAttachmentStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["BidPackageAttachmentsStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<Attachments.IBidPackageAttachmentStore, Attachments.NullBidPackageAttachmentStore>();
        else
            services.AddSingleton<Attachments.IBidPackageAttachmentStore>(
                _ => new Attachments.AzureBlobBidPackageAttachmentStore(connectionString));
    }
}
