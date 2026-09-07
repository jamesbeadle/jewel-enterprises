
namespace Jewel.JPMS.Features.Procurement;

public static class ProcurementRouteRegistration
{
    public static IServiceCollection AddProcurementReadModels(this IServiceCollection services)
    {
        services.AddScoped<BidPackagesReadModel>();
        services.AddScoped<ProjectWorkOrdersReadModel>();
        return services;
    }

    public static void RegisterProcurementRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListBidPackagesForProject, IReadOnlyList<BidPackage>>(
            new QueryRoute("/api/projects/{projectId}/bid-packages",
                query => $"/api/projects/{((ListBidPackagesForProject)query).ProjectId}/bid-packages"));

        queries.Register<GetBidPackageById, BidPackage?>(
            new QueryRoute("/api/bid-packages/{bidPackageId}",
                query => $"/api/bid-packages/{((GetBidPackageById)query).BidPackageId}"));

        queries.Register<ListQuotesForBidPackage, IReadOnlyList<Quote>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/quotes",
                query => $"/api/bid-packages/{((ListQuotesForBidPackage)query).BidPackageId}/quotes"));

        queries.Register<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>>(
            new QueryRoute("/api/projects/{projectId}/work-orders",
                query => $"/api/projects/{((ListProjectWorkOrders)query).ProjectId}/work-orders"));

        queries.Register<ListBidPackageRecipients, IReadOnlyList<BidPackageRecipient>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/recipients",
                query => $"/api/bid-packages/{((ListBidPackageRecipients)query).BidPackageId}/recipients"));

        queries.Register<ListBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/line-items",
                query => $"/api/bid-packages/{((ListBidPackageLineItems)query).BidPackageId}/line-items"));

        queries.Register<ListBidPackageEmails, IReadOnlyList<MailboxMessage>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/emails",
                query => $"/api/bid-packages/{((ListBidPackageEmails)query).BidPackageId}/emails"));

        // The package's verdicts on its tagged emails (Discarded / Extracted), joined client-side
        // to the live list above.
        queries.Register<ListBidPackageEmailDispositions, IReadOnlyList<BidPackageEmailDisposition>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/email-dispositions",
                query => $"/api/bid-packages/{((ListBidPackageEmailDispositions)query).BidPackageId}/email-dispositions"));

        queries.Register<ListQuoteLineItemsForBidPackage, IReadOnlyList<QuoteLineItem>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/quote-lines",
                query => $"/api/bid-packages/{((ListQuoteLineItemsForBidPackage)query).BidPackageId}/quote-lines"));

        queries.Register<SearchLocalSubcontractors, LocalSubcontractorSearchResult>(
            new QueryRoute("/api/projects/{projectId}/local-subcontractors",
                query =>
                {
                    var search = (SearchLocalSubcontractors)query;
                    var path = $"/api/projects/{search.ProjectId}/local-subcontractors?trade={Uri.EscapeDataString(search.Trade)}";
                    return string.IsNullOrEmpty(search.PageToken)
                        ? path
                        : $"{path}&pageToken={Uri.EscapeDataString(search.PageToken)}";
                }));

        // The search's front door: the trade term worked out from the package's own title and
        // details (plus the readiness gate on inviting subcontractors at all).
        queries.Register<ResolveBidPackageTrade, BidPackageTradeResolution>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/trade-resolution",
                query => $"/api/bid-packages/{((ResolveBidPackageTrade)query).BidPackageId}/trade-resolution"));

        // Attachments kept on a work order for record keeping (uploads travel as multipart
        // through HttpWorkOrderAttachmentStore, not through this table).
        queries.Register<ListWorkOrderAttachments, IReadOnlyList<WorkOrderAttachment>>(
            new QueryRoute("/api/work-orders/{workOrderId}/attachments",
                query => $"/api/work-orders/{((ListWorkOrderAttachments)query).WorkOrderId}/attachments"));

        queries.Register<ListBidPackageDrawings, IReadOnlyList<Drawing>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/drawings",
                query => $"/api/bid-packages/{((ListBidPackageDrawings)query).BidPackageId}/drawings"));

        // Tender-document attachments (uploads travel as multipart through
        // HttpBidPackageAttachmentStore, not through this table).
        queries.Register<ListBidPackageAttachments, IReadOnlyList<BidPackageAttachment>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/attachments",
                query => $"/api/bid-packages/{((ListBidPackageAttachments)query).BidPackageId}/attachments"));

        commands.Register<RemoveBidPackageAttachment, IReadOnlyList<BidPackageAttachment>>(
            new CommandRoute("DELETE", "/api/bid-packages/{bidPackageId}/attachments/{attachmentId}",
                command =>
                {
                    var c = (RemoveBidPackageAttachment)command;
                    return $"/api/bid-packages/{c.BidPackageId}/attachments/{c.BidPackageAttachmentId}";
                }));

        commands.Register<RemoveWorkOrderAttachment, IReadOnlyList<WorkOrderAttachment>>(
            new CommandRoute("DELETE", "/api/work-orders/{workOrderId}/attachments/{attachmentId}",
                command =>
                {
                    var c = (RemoveWorkOrderAttachment)command;
                    return $"/api/work-orders/{c.WorkOrderId}/attachments/{c.WorkOrderAttachmentId}";
                }));

        commands.Register<CreateBidPackage, BidPackage>(
            new CommandRoute("POST", "/api/projects/{projectId}/bid-packages",
                command => $"/api/projects/{((CreateBidPackage)command).ProjectId}/bid-packages"));

        // AI proposals for what to tender next (read-only — nothing is created until the user
        // picks suggestions and CreateBidPackage runs for each).
        commands.Register<SuggestBidPackages, BidPackageSuggestionResult>(
            new CommandRoute("POST", "/api/projects/{projectId}/bid-packages/suggest",
                command => $"/api/projects/{((SuggestBidPackages)command).ProjectId}/bid-packages/suggest"));

        commands.Register<CreateBidPackageFromMessage, BidPackage>(
            new CommandRoute("POST", "/api/mailbox/message/create-bid-package",
                _ => "/api/mailbox/message/create-bid-package"));

        commands.Register<CreateWorkOrderFromMessage, WorkOrder>(
            new CommandRoute("POST", "/api/mailbox/message/create-work-order",
                _ => "/api/mailbox/message/create-work-order"));

        commands.Register<InviteSubcontractorsToBidPackage, IReadOnlyList<BidPackageRecipient>>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/recipients",
                command => $"/api/bid-packages/{((InviteSubcontractorsToBidPackage)command).BidPackageId}/recipients"));

        commands.Register<RemoveBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>(
            new CommandRoute("DELETE", "/api/bid-packages/{bidPackageId}/recipients/{recipientId}",
                command =>
                {
                    var c = (RemoveBidPackageRecipient)command;
                    return $"/api/bid-packages/{c.BidPackageId}/recipients/{c.RecipientId}";
                }));

        commands.Register<DeclineBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/recipients/{recipientId}/decline",
                command =>
                {
                    var c = (DeclineBidPackageRecipient)command;
                    return $"/api/bid-packages/{c.BidPackageId}/recipients/{c.RecipientId}/decline";
                }));

        commands.Register<SetBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>(
            new CommandRoute("PUT", "/api/bid-packages/{bidPackageId}/line-items",
                command => $"/api/bid-packages/{((SetBidPackageLineItems)command).BidPackageId}/line-items"));

        commands.Register<AddBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/line-items",
                command => $"/api/bid-packages/{((AddBidPackageLineItems)command).BidPackageId}/line-items"));

        commands.Register<SetBidPackageLineItemCoverage, IReadOnlyList<BidPackageLineItem>>(
            new CommandRoute("PUT", "/api/bid-package-line-items/{lineItemId}/coverage",
                command => $"/api/bid-package-line-items/{((SetBidPackageLineItemCoverage)command).LineItemId}/coverage"));

        commands.Register<UpdateBidPackageScope, BidPackage>(
            new CommandRoute("PUT", "/api/bid-packages/{bidPackageId}",
                command => $"/api/bid-packages/{((UpdateBidPackageScope)command).BidPackageId}"));

        // Deletes a package raised in error (or an unwanted AI suggestion) with everything
        // under it; refused server-side for Awarded packages and anything a work order names.
        commands.Register<DeleteBidPackage, Contracts.Cqrs.Acknowledgement>(
            new CommandRoute("DELETE", "/api/bid-packages/{bidPackageId}",
                command => $"/api/bid-packages/{((DeleteBidPackage)command).BidPackageId}"));

        commands.Register<CloseBidPackage, BidPackage>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/close",
                command => $"/api/bid-packages/{((CloseBidPackage)command).BidPackageId}/close"));

        commands.Register<ReopenBidPackage, BidPackage>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/reopen",
                command => $"/api/bid-packages/{((ReopenBidPackage)command).BidPackageId}/reopen"));

        commands.Register<SetBidPackageDrawings, IReadOnlyList<Drawing>>(
            new CommandRoute("PUT", "/api/bid-packages/{bidPackageId}/drawings",
                command => $"/api/bid-packages/{((SetBidPackageDrawings)command).BidPackageId}/drawings"));

        commands.Register<PrepareBidPackageInviteDraft, BidPackageInviteDraft>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/draft-invite",
                command => $"/api/bid-packages/{((PrepareBidPackageInviteDraft)command).BidPackageId}/draft-invite"));

        // The in-app invite composer (2026-08-16): read/save the draft persisted on the package,
        // and SEND the invite from the projects mailbox — no trip to Outlook.
        queries.Register<GetBidPackageInviteComposerDraft, BidPackageInviteComposerDraft?>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/invite-draft",
                query => $"/api/bid-packages/{((GetBidPackageInviteComposerDraft)query).BidPackageId}/invite-draft"));

        commands.Register<SaveBidPackageInviteComposerDraft, Contracts.Cqrs.Acknowledgement>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/invite-draft",
                command => $"/api/bid-packages/{((SaveBidPackageInviteComposerDraft)command).BidPackageId}/invite-draft"));

        commands.Register<SendBidPackageInvite, BidPackageInviteSendOutcome>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/send-invite",
                command => $"/api/bid-packages/{((SendBidPackageInvite)command).BidPackageId}/send-invite"));

        commands.Register<PrepareWorkOrderEmailDraft, WorkOrderEmailDraft>(
            new CommandRoute("POST", "/api/work-orders/{workOrderId}/draft-email",
                command => $"/api/work-orders/{((PrepareWorkOrderEmailDraft)command).WorkOrderId}/draft-email"));

        commands.Register<SendWorkOrderPoEmail, WorkOrderPoEmailOutcome>(
            new CommandRoute("POST", "/api/work-orders/{workOrderId}/send-po-email",
                command => $"/api/work-orders/{((SendWorkOrderPoEmail)command).WorkOrderId}/send-po-email"));

        commands.Register<ExtractTenderFromMessage, TenderExtraction>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/extract-tender",
                command => $"/api/bid-packages/{((ExtractTenderFromMessage)command).BidPackageId}/extract-tender"));

        commands.Register<RecordTenderResponse, IReadOnlyList<BidPackageRecipient>>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/tender-response",
                command => $"/api/bid-packages/{((RecordTenderResponse)command).BidPackageId}/tender-response"));

        commands.Register<SaveExtractedQuote, Quote>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/extracted-quotes",
                command => $"/api/bid-packages/{((SaveExtractedQuote)command).BidPackageId}/extracted-quotes"));

        commands.Register<SetBidPackageEmailDisposition, IReadOnlyList<BidPackageEmailDisposition>>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/email-dispositions",
                command => $"/api/bid-packages/{((SetBidPackageEmailDisposition)command).BidPackageId}/email-dispositions"));

        commands.Register<SubmitQuoteForBidPackage, Quote>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/quotes",
                command => $"/api/bid-packages/{((SubmitQuoteForBidPackage)command).BidPackageId}/quotes"));

        commands.Register<ReviseQuote, Quote>(
            new CommandRoute("PUT", "/api/quotes/{quoteId}",
                command => $"/api/quotes/{((ReviseQuote)command).QuoteId}"));

        commands.Register<AwardBidPackage, WorkOrder>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/award",
                command => $"/api/bid-packages/{((AwardBidPackage)command).BidPackageId}/award"));

        commands.Register<UpdateWorkOrder, WorkOrder>(
            new CommandRoute("PUT", "/api/work-orders/{workOrderId}",
                command => $"/api/work-orders/{((UpdateWorkOrder)command).WorkOrderId}"));

        // Raises a work order directly — no bid package — with per-centre priced lines.
        commands.Register<CreateManualWorkOrder, WorkOrder>(
            new CommandRoute("POST", "/api/projects/{projectId}/work-orders",
                command => $"/api/projects/{((CreateManualWorkOrder)command).ProjectId}/work-orders"));

        // Edits a manually raised order wholesale — supplier, title, scope and priced lines.
        commands.Register<UpdateManualWorkOrder, WorkOrder>(
            new CommandRoute("PUT", "/api/projects/{projectId}/work-orders/{workOrderId}",
                command =>
                {
                    var c = (UpdateManualWorkOrder)command;
                    return $"/api/projects/{c.ProjectId}/work-orders/{c.WorkOrderId}";
                }));

        // Approves a draft order — mints its sequential number and makes it live.
        commands.Register<ApproveWorkOrder, WorkOrder>(
            new CommandRoute("POST", "/api/projects/{projectId}/work-orders/{workOrderId}/approve",
                command =>
                {
                    var c = (ApproveWorkOrder)command;
                    return $"/api/projects/{c.ProjectId}/work-orders/{c.WorkOrderId}/approve";
                }));

        // Rejects a draft order — terminal; it stops counting anywhere.
        commands.Register<RejectWorkOrder, WorkOrder>(
            new CommandRoute("POST", "/api/projects/{projectId}/work-orders/{workOrderId}/reject",
                command =>
                {
                    var c = (RejectWorkOrder)command;
                    return $"/api/projects/{c.ProjectId}/work-orders/{c.WorkOrderId}/reject";
                }));

        // Deletes a draft raised in error (or duplicated by the assistant) outright — no
        // Rejected row is kept; the audit trail carries the surviving record. Draft-only,
        // refused server-side for anything decided.
        commands.Register<DeleteDraftWorkOrder, Contracts.Cqrs.Acknowledgement>(
            new CommandRoute("DELETE", "/api/projects/{projectId}/work-orders/{workOrderId}",
                command =>
                {
                    var c = (DeleteDraftWorkOrder)command;
                    return $"/api/projects/{c.ProjectId}/work-orders/{c.WorkOrderId}";
                }));

        // Cancels a released order — terminal, keeps its number as the record of a voided
        // purchase order; its value stops counting everywhere. Directors / FD only.
        commands.Register<CancelWorkOrder, WorkOrder>(
            new CommandRoute("POST", "/api/projects/{projectId}/work-orders/{workOrderId}/cancel",
                command =>
                {
                    var c = (CancelWorkOrder)command;
                    return $"/api/projects/{c.ProjectId}/work-orders/{c.WorkOrderId}/cancel";
                }));

        // Re-codes / splits one priced line across cost centres, by £ amount.
        commands.Register<RecodeWorkOrderLine, IReadOnlyList<WorkOrderLine>>(
            new CommandRoute("POST", "/api/projects/{projectId}/work-order-lines/{lineId}/recode",
                command =>
                {
                    var c = (RecodeWorkOrderLine)command;
                    return $"/api/projects/{c.ProjectId}/work-order-lines/{c.WorkOrderLineId}/recode";
                }));
        // Issues the new work order that instructs an approved variation order.
        commands.Register<IssueWorkOrderForVariationOrder, WorkOrder>(
            new CommandRoute("POST", "/api/variation-orders/{variationOrderId}/work-order",
                command => $"/api/variation-orders/{((IssueWorkOrderForVariationOrder)command).VariationOrderId}/work-order"));

    }
}
