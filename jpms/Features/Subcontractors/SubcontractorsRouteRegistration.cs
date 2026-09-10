using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Features.Subcontractors;

public static class SubcontractorsRouteRegistration
{
    public static IServiceCollection AddSubcontractorsReadModels(this IServiceCollection services)
    {
        services.AddScoped<SubcontractorsReadModel>();
        services.AddScoped<TradesReadModel>();
        services.AddScoped<ComplianceOverviewReadModel>();
        return services;
    }

    public static void RegisterSubcontractorsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListSubcontractors, IReadOnlyList<Subcontractor>>(QueryRoute.Static("/api/subcontractors"));

        queries.Register<ListTrades, IReadOnlyList<Trade>>(QueryRoute.Static("/api/trades"));

        commands.Register<AddTrade, Trade>(CommandRoute.Post("/api/trades"));

        commands.Register<RenameTrade, Trade>(
            new CommandRoute("PUT", "/api/trades/{tradeId}",
                command => $"/api/trades/{((RenameTrade)command).TradeId}"));

        commands.Register<DeleteTrade, Acknowledgement>(
            new CommandRoute("DELETE", "/api/trades/{tradeId}",
                command => $"/api/trades/{((DeleteTrade)command).TradeId}"));

        queries.Register<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>>(
            QueryRoute.Static("/api/compliance-documents"));

        queries.Register<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>>(
            new QueryRoute("/api/subcontractors/{subcontractorId}/compliance",
                query => $"/api/subcontractors/{((ListComplianceDocumentsForSubcontractor)query).SubcontractorId}/compliance"));

        queries.Register<GetSubcontractorStatement, SubcontractorStatement>(
            new QueryRoute("/api/subcontractors/{subcontractorId}/statement",
                query => $"/api/subcontractors/{((GetSubcontractorStatement)query).SubcontractorId}/statement"));

        commands.Register<PrepareSubcontractorStatementEmailDraft, SubcontractorStatementEmailDraft>(
            new CommandRoute("POST", "/api/subcontractors/{subcontractorId}/statement/draft-email",
                command => $"/api/subcontractors/{((PrepareSubcontractorStatementEmailDraft)command).SubcontractorId}/statement/draft-email"));

        commands.Register<AddSubcontractorToDirectory, Subcontractor>(CommandRoute.Post("/api/subcontractors"));

        commands.Register<PromoteSubcontractorToDirectory, Subcontractor>(
            new CommandRoute("POST", "/api/subcontractors/{subcontractorId}/promote",
                command => $"/api/subcontractors/{((PromoteSubcontractorToDirectory)command).SubcontractorId}/promote"));

        // Xero import + consolidation (the duplicate-resolution flow) + company contacts.
        commands.Register<ImportXeroSupplier, Subcontractor>(CommandRoute.Post("/api/subcontractors/import-from-xero"));

        commands.Register<LinkDirectoryRecordToXeroContact, Subcontractor>(
            new CommandRoute("POST", "/api/subcontractors/{subcontractorId}/xero-link",
                command => $"/api/subcontractors/{((LinkDirectoryRecordToXeroContact)command).SubcontractorId}/xero-link"));

        queries.Register<PreviewXeroContactPush, XeroContactPushPreview>(
            new QueryRoute("/api/subcontractors/{subcontractorId}/xero-contact-push",
                query => $"/api/subcontractors/{((PreviewXeroContactPush)query).SubcontractorId}/xero-contact-push"));

        commands.Register<PushDirectoryContactsToXeroContact, XeroContactPushOutcome>(
            new CommandRoute("POST", "/api/subcontractors/{subcontractorId}/xero-contact-push",
                command => $"/api/subcontractors/{((PushDirectoryContactsToXeroContact)command).SubcontractorId}/xero-contact-push"));

        commands.Register<UnlinkDirectoryRecordFromXeroContact, Subcontractor>(
            new CommandRoute("DELETE", "/api/subcontractors/{subcontractorId}/xero-link/{xeroContactId}",
                command =>
                {
                    var c = (UnlinkDirectoryRecordFromXeroContact)command;
                    return $"/api/subcontractors/{c.SubcontractorId}/xero-link/{c.XeroContactId}";
                }));

        commands.Register<ConsolidateDirectoryRecords, Subcontractor>(CommandRoute.Post("/api/subcontractors/consolidate"));

        queries.Register<ListCompanyContacts, IReadOnlyList<CompanyContact>>(
            new QueryRoute("/api/subcontractors/{subcontractorId}/contacts",
                query => $"/api/subcontractors/{((ListCompanyContacts)query).SubcontractorId}/contacts"));

        commands.Register<UpsertCompanyContact, CompanyContact>(
            new CommandRoute("POST", "/api/subcontractors/{subcontractorId}/contacts",
                command => $"/api/subcontractors/{((UpsertCompanyContact)command).SubcontractorId}/contacts"));

        commands.Register<RemoveCompanyContact, Acknowledgement>(
            new CommandRoute("DELETE", "/api/subcontractors/{subcontractorId}/contacts/{companyContactId}",
                command =>
                {
                    var c = (RemoveCompanyContact)command;
                    return $"/api/subcontractors/{c.SubcontractorId}/contacts/{c.CompanyContactId}";
                }));

        commands.Register<RecordCisVerification, Subcontractor>(
            new CommandRoute("PUT", "/api/subcontractors/{subcontractorId}/cis-verification",
                command => $"/api/subcontractors/{((RecordCisVerification)command).SubcontractorId}/cis-verification"));

        commands.Register<UpdateSubcontractor, Subcontractor>(
            new CommandRoute("PUT", "/api/subcontractors/{subcontractorId}",
                command => $"/api/subcontractors/{((UpdateSubcontractor)command).SubcontractorId}"));

        commands.Register<UploadComplianceDocument, ComplianceDocument>(
            new CommandRoute("POST", "/api/subcontractors/{subcontractorId}/compliance",
                command => $"/api/subcontractors/{((UploadComplianceDocument)command).SubcontractorId}/compliance"));

        commands.Register<SetComplianceDocumentDetails, ComplianceDocument>(
            new CommandRoute("PUT", "/api/subcontractors/{subcontractorId}/compliance/{complianceDocumentId}/details",
                command => $"/api/subcontractors/{((SetComplianceDocumentDetails)command).SubcontractorId}/compliance/{((SetComplianceDocumentDetails)command).ComplianceDocumentId}/details"));
    }
}
