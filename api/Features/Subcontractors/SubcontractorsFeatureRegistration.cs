using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Api.Features.Subcontractors.Queries;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;
using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Subcontractors;

public static class SubcontractorsFeatureRegistration
{
    public static IServiceCollection AddSubcontractorsFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterBlobStore(services, configuration);

        services.AddScoped<IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>>, ListSubcontractorsHandler>();
        services.AddScoped<IQueryHandler<ListTrades, IReadOnlyList<Trade>>, ListTradesHandler>();
        services.AddScoped<IQueryHandler<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>>, ListComplianceDocumentsForSubcontractorHandler>();
        services.AddScoped<IQueryHandler<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>>, ListCurrentComplianceDocumentsHandler>();
        services.AddScoped<IQueryHandler<GetSubcontractorStatement, SubcontractorStatement>, GetSubcontractorStatementHandler>();

        services.AddScoped<ICommandHandler<PrepareSubcontractorStatementEmailDraft, SubcontractorStatementEmailDraft>, PrepareSubcontractorStatementEmailDraftHandler>();
        services.AddScoped<PrepareSubcontractorStatementEmailDraftAuthorisation>();
        services.AddScoped<PrepareSubcontractorStatementEmailDraftValidation>();

        services.AddScoped<ICommandHandler<AddSubcontractorToDirectory, Subcontractor>, AddSubcontractorToDirectoryHandler>();
        services.AddScoped<AddSubcontractorToDirectoryAuthorisation>();
        services.AddScoped<AddSubcontractorToDirectoryValidation>();

        services.AddScoped<ICommandHandler<UpdateSubcontractor, Subcontractor>, UpdateSubcontractorHandler>();
        services.AddScoped<UpdateSubcontractorAuthorisation>();
        services.AddScoped<UpdateSubcontractorValidation>();

        services.AddScoped<ICommandHandler<RecordCisVerification, Subcontractor>, RecordCisVerificationHandler>();
        services.AddScoped<RecordCisVerificationAuthorisation>();
        services.AddScoped<RecordCisVerificationValidation>();

        services.AddScoped<ICommandHandler<AddTrade, Trade>, AddTradeHandler>();
        services.AddScoped<AddTradeAuthorisation>();
        services.AddScoped<AddTradeValidation>();

        services.AddScoped<ICommandHandler<RenameTrade, Trade>, RenameTradeHandler>();
        services.AddScoped<RenameTradeAuthorisation>();
        services.AddScoped<RenameTradeValidation>();

        services.AddScoped<ICommandHandler<DeleteTrade, Acknowledgement>, DeleteTradeHandler>();
        services.AddScoped<DeleteTradeAuthorisation>();
        services.AddScoped<DeleteTradeValidation>();

        services.AddScoped<ICommandHandler<PromoteSubcontractorToDirectory, Subcontractor>, PromoteSubcontractorToDirectoryHandler>();
        services.AddScoped<PromoteSubcontractorToDirectoryAuthorisation>();
        services.AddScoped<PromoteSubcontractorToDirectoryValidation>();

        services.AddScoped<ICommandHandler<UploadComplianceDocument, ComplianceDocument>, UploadComplianceDocumentHandler>();
        services.AddScoped<UploadComplianceDocumentAuthorisation>();
        services.AddScoped<UploadComplianceDocumentValidation>();
        services.AddScoped<UploadComplianceDocumentFileAuthorisation>();

        services.AddScoped<ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument>, AddComplianceDocumentVersionHandler>();

        // Xero import + link/unlink on an existing record + consolidation (the duplicate-resolution
        // flow) + company contacts.
        services.AddScoped<XeroSupplierLookup>();
        services.AddScoped<ICommandHandler<ImportXeroSupplier, Subcontractor>, ImportXeroSupplierHandler>();
        services.AddScoped<ImportXeroSupplierAuthorisation>();
        services.AddScoped<ImportXeroSupplierValidation>();

        services.AddScoped<ICommandHandler<LinkDirectoryRecordToXeroContact, Subcontractor>, LinkDirectoryRecordToXeroContactHandler>();
        services.AddScoped<LinkDirectoryRecordToXeroContactAuthorisation>();
        services.AddScoped<LinkDirectoryRecordToXeroContactValidation>();

        services.AddScoped<ICommandHandler<UnlinkDirectoryRecordFromXeroContact, Subcontractor>, UnlinkDirectoryRecordFromXeroContactHandler>();
        services.AddScoped<UnlinkDirectoryRecordFromXeroContactAuthorisation>();
        services.AddScoped<UnlinkDirectoryRecordFromXeroContactValidation>();

        services.AddScoped<IQueryHandler<PreviewXeroContactPush, XeroContactPushPreview>, PreviewXeroContactPushHandler>();
        services.AddScoped<ICommandHandler<PushDirectoryContactsToXeroContact, XeroContactPushOutcome>, PushDirectoryContactsToXeroContactHandler>();
        services.AddScoped<PushDirectoryContactsToXeroContactAuthorisation>();
        services.AddScoped<PushDirectoryContactsToXeroContactValidation>();

        services.AddScoped<ICommandHandler<ConsolidateDirectoryRecords, Subcontractor>, ConsolidateDirectoryRecordsHandler>();
        services.AddScoped<ConsolidateDirectoryRecordsAuthorisation>();
        services.AddScoped<ConsolidateDirectoryRecordsValidation>();

        services.AddScoped<IQueryHandler<ListCompanyContacts, IReadOnlyList<CompanyContact>>, ListCompanyContactsHandler>();
        services.AddScoped<ICommandHandler<UpsertCompanyContact, CompanyContact>, UpsertCompanyContactHandler>();
        services.AddScoped<ICommandHandler<RemoveCompanyContact, Acknowledgement>, RemoveCompanyContactHandler>();
        services.AddScoped<UpsertCompanyContactAuthorisation>();
        services.AddScoped<UpsertCompanyContactValidation>();

        services.AddScoped<SubcontractorPortalInviter>();
        services.AddScoped<InviteSubcontractorPortalUserAuthorisation>();

        return services;
    }

    // Mirrors the drawings feature: private container, loud NullStore when unconfigured.
    // The DrawingsStorage fallback matches every other blob feature (Document Control, Building
    // Control, work-order attachments…): prod's SWA configures only DrawingsStorage:ConnectionString,
    // and on SWA managed functions AzureWebJobsStorage is the platform's own account — not ours —
    // which is what 500'd every compliance download through the portal (2026-08-31).
    private static void RegisterBlobStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ComplianceStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IComplianceBlobStore, NullComplianceBlobStore>();
        else
            services.AddSingleton<IComplianceBlobStore>(_ => new AzureBlobComplianceStore(connectionString));
    }
}
