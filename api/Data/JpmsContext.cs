using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data;

public sealed partial class JpmsContext : DbContext
{
    public JpmsContext(DbContextOptions<JpmsContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }

    public DbSet<DirectoryUserEntity> DirectoryUsers => Set<DirectoryUserEntity>();
    public DbSet<DirectoryUserRoleEntity> DirectoryUserRoles => Set<DirectoryUserRoleEntity>();
    public DbSet<AccessRequestEntity> AccessRequests => Set<AccessRequestEntity>();

    public DbSet<UserCredentialEntity> UserCredentials => Set<UserCredentialEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();
    public DbSet<UserSessionEntity> UserSessions => Set<UserSessionEntity>();

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<ProjectContactEntity> ProjectContacts => Set<ProjectContactEntity>();
    public DbSet<PartyContactEntity> PartyContacts => Set<PartyContactEntity>();
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<ArchitectEntity> Architects => Set<ArchitectEntity>();
    public DbSet<LeadEntity> Leads => Set<LeadEntity>();
    public DbSet<QualificationAssessmentEntity> QualificationAssessments => Set<QualificationAssessmentEntity>();
    public DbSet<SiteVisitEntity> SiteVisits => Set<SiteVisitEntity>();
    public DbSet<InfoChaseItemEntity> InfoChaseItems => Set<InfoChaseItemEntity>();
    public DbSet<BidDecisionEntity> BidDecisions => Set<BidDecisionEntity>();
    public DbSet<ProposalEntity> Proposals => Set<ProposalEntity>();
    public DbSet<LeadOutcomeEntity> LeadOutcomes => Set<LeadOutcomeEntity>();
    // Sales (2026-09-06): strategies for finding leads, and each lead's timeline.
    public DbSet<SalesStrategyEntity> SalesStrategies => Set<SalesStrategyEntity>();
    public DbSet<LeadActivityEntity> LeadActivities => Set<LeadActivityEntity>();
    public DbSet<ImagineRoundEntity> ImagineRounds => Set<ImagineRoundEntity>();
    public DbSet<ImagineImageEntity> ImagineImages => Set<ImagineImageEntity>();
    public DbSet<SalesProposalEntity> SalesProposals => Set<SalesProposalEntity>();

    public DbSet<BoqLineItemEntity> BoqLineItems => Set<BoqLineItemEntity>();
    public DbSet<BoqSignOffEntity> BoqSignOffs => Set<BoqSignOffEntity>();
    public DbSet<RateEntity> Rates => Set<RateEntity>();
    public DbSet<CostCodeEntity> CostCodes => Set<CostCodeEntity>();
    public DbSet<WalkRoundNoteEntity> WalkRoundNotes => Set<WalkRoundNoteEntity>();

    public DbSet<DrawingEntity> Drawings => Set<DrawingEntity>();
    public DbSet<DrawingRevisionEntity> DrawingRevisions => Set<DrawingRevisionEntity>();
    public DbSet<DrawingIssueRecordEntity> DrawingIssueRecords => Set<DrawingIssueRecordEntity>();
    public DbSet<DrawingFolderEntity> DrawingFolders => Set<DrawingFolderEntity>();
    public DbSet<BluebeamConnectionEntity> BluebeamConnections => Set<BluebeamConnectionEntity>();
    public DbSet<DrawingExtractionEntity> DrawingExtractions => Set<DrawingExtractionEntity>();
    public DbSet<DrawingMarkupEntity> DrawingMarkups => Set<DrawingMarkupEntity>();

    public DbSet<DocumentControlItemEntity> DocumentControlItems => Set<DocumentControlItemEntity>();
    public DbSet<PaymentCertificateEntity> PaymentCertificates => Set<PaymentCertificateEntity>();

    public DbSet<SubcontractorEntity> Subcontractors => Set<SubcontractorEntity>();
    public DbSet<TradeEntity> Trades => Set<TradeEntity>();
    public DbSet<SubcontractorTradeEntity> SubcontractorTrades => Set<SubcontractorTradeEntity>();
    public DbSet<SubcontractorXeroLinkEntity> SubcontractorXeroLinks => Set<SubcontractorXeroLinkEntity>();
    public DbSet<CompanyContactEntity> CompanyContacts => Set<CompanyContactEntity>();
    public DbSet<ComplianceDocumentEntity> ComplianceDocuments => Set<ComplianceDocumentEntity>();

    public DbSet<HsRecordEntity> HsRecords => Set<HsRecordEntity>();
    public DbSet<HsRecordAttendanceEntity> HsRecordAttendance => Set<HsRecordAttendanceEntity>();
    public DbSet<MobilisationItemEntity> MobilisationItems => Set<MobilisationItemEntity>();

    public DbSet<BidPackageEntity> BidPackages => Set<BidPackageEntity>();
    public DbSet<BidPackageRecipientEntity> BidPackageRecipients => Set<BidPackageRecipientEntity>();
    public DbSet<BidPackageLineItemEntity> BidPackageLineItems => Set<BidPackageLineItemEntity>();
    public DbSet<QuoteEntity> Quotes => Set<QuoteEntity>();
    public DbSet<QuoteLineItemEntity> QuoteLineItems => Set<QuoteLineItemEntity>();
    public DbSet<BidPackageDrawingEntity> BidPackageDrawings => Set<BidPackageDrawingEntity>();
    public DbSet<BidPackageAttachmentEntity> BidPackageAttachments => Set<BidPackageAttachmentEntity>();
    public DbSet<BidPackageEmailDispositionEntity> BidPackageEmailDispositions => Set<BidPackageEmailDispositionEntity>();
    public DbSet<TenderEnquiryEntity> TenderEnquiries => Set<TenderEnquiryEntity>();
    public DbSet<TenderEnquiryAnswerEntity> TenderEnquiryAnswers => Set<TenderEnquiryAnswerEntity>();
    public DbSet<TenderEnquiryAttachmentEntity> TenderEnquiryAttachments => Set<TenderEnquiryAttachmentEntity>();
    public DbSet<WorkOrderEntity> WorkOrders => Set<WorkOrderEntity>();
    public DbSet<WorkOrderLineEntity> WorkOrderLines => Set<WorkOrderLineEntity>();
    public DbSet<WorkOrderAttachmentEntity> WorkOrderAttachments => Set<WorkOrderAttachmentEntity>();
    public DbSet<VariationOrderEntity> VariationOrders => Set<VariationOrderEntity>();
    public DbSet<VariationOrderMessageEntity> VariationOrderMessages => Set<VariationOrderMessageEntity>();
    public DbSet<SubcontractorVariationRequestEntity> SubcontractorVariationRequests => Set<SubcontractorVariationRequestEntity>();
    public DbSet<RequestEntity> Requests => Set<RequestEntity>();
    public DbSet<RequestItemEntity> RequestItems => Set<RequestItemEntity>();
    public DbSet<RequestMessageEntity> RequestMessages => Set<RequestMessageEntity>();
    public DbSet<CostCenterEntity> CostCenters => Set<CostCenterEntity>();

    public DbSet<XeroLedgerLineEntity> XeroLedgerLines => Set<XeroLedgerLineEntity>();
    public DbSet<XeroCostSplitEntity> XeroCostSplits => Set<XeroCostSplitEntity>();
    public DbSet<XeroLineWorkOrderLinkEntity> XeroLineWorkOrderLinks => Set<XeroLineWorkOrderLinkEntity>();
    // The discussion thread on disputed ledger lines (the allocation page's Disputed bucket).
    public DbSet<XeroDisputeMessageEntity> XeroDisputeMessages => Set<XeroDisputeMessageEntity>();

    // Work Order bill approvals (2026-09-08): one row per Approve on the allocation page's Work
    // Order bills tab — the undo's handle and the audit's "which rule matched".
    public DbSet<WorkOrderBillApprovalEntity> WorkOrderBillApprovals => Set<WorkOrderBillApprovalEntity>();

    // The stored site P&L: per project per month, from Xero's profit & loss report filtered by
    // the project's Sites tracking option — feeds the Profit Summary's cumulative chart.
    public DbSet<XeroSitePnlMonthEntity> XeroSitePnlMonths => Set<XeroSitePnlMonthEntity>();

    public DbSet<TodoItemEntity> TodoItems => Set<TodoItemEntity>();
    // Undirected to-do ↔ to-do links, one row per pair in canonical id order (TodoItemLinkPairs).
    public DbSet<TodoItemLinkEntity> TodoItemLinks => Set<TodoItemLinkEntity>();
    // The per-item timeline (created / started / chased / reassigned / … / email sent).
    public DbSet<TodoItemActivityEntity> TodoItemActivities => Set<TodoItemActivityEntity>();

    // Project calendar entries (site visits, deliveries, meetings, attendance) — the Calendar tab.
    public DbSet<CalendarEventEntity> CalendarEvents => Set<CalendarEventEntity>();

    // Internal-only titled free-text notes per project (door codes, site notes) — the Useful
    // Information tab.
    public DbSet<UsefulInformationNoteEntity> UsefulInformationNotes => Set<UsefulInformationNoteEntity>();

    public DbSet<LadClaimEntity> LadClaims => Set<LadClaimEntity>();

    public DbSet<SiteReportEntity> SiteReports => Set<SiteReportEntity>();
    public DbSet<ProgrammeTaskEntity> ProgrammeTasks => Set<ProgrammeTaskEntity>();
    public DbSet<ProgrammeTaskLinkEntity> ProgrammeTaskLinks => Set<ProgrammeTaskLinkEntity>();
    public DbSet<ProgrammeBaselineEntity> ProgrammeBaselines => Set<ProgrammeBaselineEntity>();
    public DbSet<ProgrammeBaselineTaskEntity> ProgrammeBaselineTasks => Set<ProgrammeBaselineTaskEntity>();
    // Draft programme updates from the certified valuation (2026-09-08): a task's confirmed
    // cost-centre mappings, and the drafts (with their per-task lines) awaiting review.
    public DbSet<ProgrammeTaskCostCentreEntity> ProgrammeTaskCostCentres => Set<ProgrammeTaskCostCentreEntity>();
    public DbSet<ProgrammeDraftEntity> ProgrammeDrafts => Set<ProgrammeDraftEntity>();
    public DbSet<ProgrammeDraftLineEntity> ProgrammeDraftLines => Set<ProgrammeDraftLineEntity>();
    public DbSet<PhotoEntity> Photos => Set<PhotoEntity>();

    public DbSet<ProgressUpdateEntity> ProgressUpdates => Set<ProgressUpdateEntity>();
    public DbSet<ProgressPhotoEntity> ProgressPhotos => Set<ProgressPhotoEntity>();
    public DbSet<ProgressReportEntity> ProgressReports => Set<ProgressReportEntity>();
    public DbSet<ProgressReportSelectionEntity> ProgressReportSelections => Set<ProgressReportSelectionEntity>();

    public DbSet<ClaimPeriodEntity> ClaimPeriods => Set<ClaimPeriodEntity>();
    public DbSet<ValuationEntity> Valuations => Set<ValuationEntity>();
    public DbSet<ValuationLineItemEntity> ValuationLineItems => Set<ValuationLineItemEntity>();
    public DbSet<ValuationClaimEntity> ValuationClaims => Set<ValuationClaimEntity>();
    public DbSet<ClaimLineEntity> ClaimLines => Set<ClaimLineEntity>();
    public DbSet<CvrSnapshotEntity> CvrSnapshots => Set<CvrSnapshotEntity>();
    public DbSet<CvrPackageRowEntity> CvrPackageRows => Set<CvrPackageRowEntity>();
    public DbSet<ForecastComponentEntity> ForecastComponents => Set<ForecastComponentEntity>();
    public DbSet<QsAccrualEntity> QsAccruals => Set<QsAccrualEntity>();
    public DbSet<PrelimItemEntity> PrelimItems => Set<PrelimItemEntity>();
    public DbSet<PrelimForecastEntryEntity> PrelimForecastEntries => Set<PrelimForecastEntryEntity>();
    public DbSet<EotEntity> Eots => Set<EotEntity>();
    public DbSet<CostCodeBudgetEntity> CostCodeBudgets => Set<CostCodeBudgetEntity>();
    public DbSet<CostCentreCostProgressEntity> CostCentreCostProgress => Set<CostCentreCostProgressEntity>();
    public DbSet<CostCentreGroupEntity> CostCentreGroups => Set<CostCentreGroupEntity>();
    public DbSet<CostCentreGroupMemberEntity> CostCentreGroupMembers => Set<CostCentreGroupMemberEntity>();
    public DbSet<ReconciliationPackageEntity> ReconciliationPackages => Set<ReconciliationPackageEntity>();
    public DbSet<ReconciliationPackageOrderEntity> ReconciliationPackageOrders => Set<ReconciliationPackageOrderEntity>();
    public DbSet<ReconciliationPackageSalesLineEntity> ReconciliationPackageSalesLines => Set<ReconciliationPackageSalesLineEntity>();
    public DbSet<ReconciliationPackageCostLineEntity> ReconciliationPackageCostLines => Set<ReconciliationPackageCostLineEntity>();
    public DbSet<TimesheetEntity> Timesheets => Set<TimesheetEntity>();
    public DbSet<WorkerEntity> Workers => Set<WorkerEntity>();
    public DbSet<WorkerRateHistoryEntity> WorkerRateHistories => Set<WorkerRateHistoryEntity>();
    public DbSet<ProjectWorkerAssignmentEntity> ProjectWorkerAssignments => Set<ProjectWorkerAssignmentEntity>();
    public DbSet<SiteAttendanceEntity> SiteAttendances => Set<SiteAttendanceEntity>();
    public DbSet<XeroLineTimesheetCoverEntity> XeroLineTimesheetCovers => Set<XeroLineTimesheetCoverEntity>();
    public DbSet<LabourSettlementVarianceEntity> LabourSettlementVariances => Set<LabourSettlementVarianceEntity>();
    public DbSet<WorkerContractEntity> WorkerContracts => Set<WorkerContractEntity>();
    public DbSet<WorkerAbsenceEntity> WorkerAbsences => Set<WorkerAbsenceEntity>();
    public DbSet<WorkerCisStatusEntity> WorkerCisStatuses => Set<WorkerCisStatusEntity>();
    public DbSet<LabourWeekSignOffEntity> LabourWeekSignOffs => Set<LabourWeekSignOffEntity>();
    public DbSet<WorkerSettlementLineEntity> WorkerSettlementLines => Set<WorkerSettlementLineEntity>();
    public DbSet<SiteXeroMappingEntity> SiteXeroMappings => Set<SiteXeroMappingEntity>();
    public DbSet<CostCodeXeroMappingEntity> CostCodeXeroMappings => Set<CostCodeXeroMappingEntity>();
    public DbSet<XeroCodingRunEntity> XeroCodingRuns => Set<XeroCodingRunEntity>();
    public DbSet<LabourChaseDismissalEntity> LabourChaseDismissals => Set<LabourChaseDismissalEntity>();
    public DbSet<CompanyRegisterItemEntity> CompanyRegisterItems => Set<CompanyRegisterItemEntity>();
    public DbSet<PolicyDocumentEntity> PolicyDocuments => Set<PolicyDocumentEntity>();
    public DbSet<PolicySignOffEntity> PolicySignOffs => Set<PolicySignOffEntity>();
    public DbSet<CashflowSnapshotEntity> CashflowSnapshots => Set<CashflowSnapshotEntity>();
    public DbSet<WeeklyCashflowItemEntity> WeeklyCashflowItems => Set<WeeklyCashflowItemEntity>();
    public DbSet<WeeklyCashflowPlacementEntity> WeeklyCashflowPlacements => Set<WeeklyCashflowPlacementEntity>();
    public DbSet<WeeklyCashflowSupplierGroupEntity> WeeklyCashflowSupplierGroups => Set<WeeklyCashflowSupplierGroupEntity>();
    public DbSet<WeeklyCashflowExclusionEntity> WeeklyCashflowExclusions => Set<WeeklyCashflowExclusionEntity>();
    public DbSet<ValuationInvoiceEntity> ValuationInvoices => Set<ValuationInvoiceEntity>();
    public DbSet<ValuationInvoiceEventEntity> ValuationInvoiceEvents => Set<ValuationInvoiceEventEntity>();
    public DbSet<ValuationReportSnapshotEntity> ValuationReportSnapshots => Set<ValuationReportSnapshotEntity>();
    public DbSet<ValuationReportSnapshotLineEntity> ValuationReportSnapshotLines => Set<ValuationReportSnapshotLineEntity>();
    public DbSet<ClientCostReferenceEntity> ClientCostReferences => Set<ClientCostReferenceEntity>();
    public DbSet<DayworkEntity> Dayworks => Set<DayworkEntity>();
    public DbSet<ContraChargeEntity> ContraCharges => Set<ContraChargeEntity>();
    public DbSet<SubcontractorRetentionEntity> SubcontractorRetentions => Set<SubcontractorRetentionEntity>();
    public DbSet<ProjectRetentionEntity> ProjectRetentions => Set<ProjectRetentionEntity>();
    public DbSet<ProjectContractEntity> ProjectContracts => Set<ProjectContractEntity>();
    public DbSet<ProjectContractAmendmentEntity> ProjectContractAmendments => Set<ProjectContractAmendmentEntity>();

    public DbSet<AgentActivityEntity> AgentActivity => Set<AgentActivityEntity>();

    // The AI connector's OAuth state — registered client software, in-flight authorisation codes,
    // and the per-user bearer tokens the MCP endpoint accepts (docs/ai/10-mcp-connector.md).
    public DbSet<OAuthClientEntity> OAuthClients => Set<OAuthClientEntity>();
    public DbSet<OAuthAuthCodeEntity> OAuthAuthCodes => Set<OAuthAuthCodeEntity>();
    public DbSet<OAuthTokenEntity> OAuthTokens => Set<OAuthTokenEntity>();

    // The assistant's skills — the domain half of an agent, edited in the portal
    // (docs/ai/05-agents-and-skills.md). Revisions are append-only.
    public DbSet<SkillEntity> Skills => Set<SkillEntity>();
    public DbSet<SkillReferenceEntity> SkillReferences => Set<SkillReferenceEntity>();
    public DbSet<SkillRevisionEntity> SkillRevisions => Set<SkillRevisionEntity>();

    // Skills wired to connector actions — the edge describe_action resolves so attached doctrine
    // rides into the model's context with the action's schema (2026-08-31).
    public DbSet<AiActionSkillEntity> AiActionSkills => Set<AiActionSkillEntity>();

    public DbSet<DefectEntity> Defects => Set<DefectEntity>();

    // Project inventory — products held for the job and where they're kept (INV-#### tag stems).
    public DbSet<InventoryItemEntity> InventoryItems => Set<InventoryItemEntity>();
    // Site instructions — written instructions to site (SI-#### tag stems), 2026-09-03.
    public DbSet<SiteInstructionEntity> SiteInstructions => Set<SiteInstructionEntity>();

    // KPI emails — emails marked against a person (a portal user or someone added by name),
    // administrators only (2026-09-03).
    public DbSet<KpiPersonEntity> KpiPeople => Set<KpiPersonEntity>();
    public DbSet<KpiEmailEntity> KpiEmails => Set<KpiEmailEntity>();

    // Building control — the statutory sign-off trail: the case with the body, its inspection
    // stages, and the files (photos, site reports, notices, the completion certificate).
    public DbSet<BuildingControlCaseEntity> BuildingControlCases => Set<BuildingControlCaseEntity>();
    public DbSet<BuildingControlInspectionEntity> BuildingControlInspections => Set<BuildingControlInspectionEntity>();
    public DbSet<BuildingControlAttachmentEntity> BuildingControlAttachments => Set<BuildingControlAttachmentEntity>();
    public DbSet<PracticalCompletionEntity> PracticalCompletions => Set<PracticalCompletionEntity>();
    public DbSet<HandoverPackItemEntity> HandoverPackItems => Set<HandoverPackItemEntity>();
    public DbSet<SettlementRecordEntity> SettlementRecords => Set<SettlementRecordEntity>();
    public DbSet<VatAnalysisEntity> VatAnalyses => Set<VatAnalysisEntity>();
    public DbSet<RetentionReleaseEntity> RetentionReleases => Set<RetentionReleaseEntity>();

    // Append-only audit trail of client-facing interactions (pathway split — see
    // docs/Pathway-Split-Platform-Flow-Plan.md §4).
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    // The announced app version — one row ("current") that Admin → System publishes and every
    // HTTP response header reports; see Features/Platform.
    public DbSet<AppVersionEntity> AppVersions => Set<AppVersionEntity>();

    // Architect's Instructions — the formal instructions that authorise varied work — plus the
    // many-to-many between them and the variations they cover.
    public DbSet<ArchitectInstructionEntity> ArchitectInstructions => Set<ArchitectInstructionEntity>();
    public DbSet<ArchitectInstructionVariationEntity> ArchitectInstructionVariations =>
        Set<ArchitectInstructionVariationEntity>();

    // Drawings and files attached to a request (site photos, marked-up details, linked revisions).
    public DbSet<RequestAttachmentEntity> RequestAttachments => Set<RequestAttachmentEntity>();
}
