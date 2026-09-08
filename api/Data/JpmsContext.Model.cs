using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data;

public sealed partial class JpmsContext
{
    /// <summary>
    /// Read-path indexes. JPMS deliberately declares no FK relationships (records link by loose
    /// string id), so EF's automatic FK-index convention never fires — every ProjectId / RequestId
    /// / WorkOrderId lookup was a table scan that grows with the data. These are the columns the
    /// hot queries actually filter and join on; each is declared here so the model, the snapshot
    /// and the database agree, and created by the AddPerformanceIndexes migration.
    ///
    /// Index names are pinned explicitly because three of them (WorkOrderLines, Timesheets,
    /// XeroLineTimesheetCovers) already exist in production — hand-run from
    /// infra/perf-financials-indexes.sql after the financials query started returning 504s — so
    /// the migration must not collide with them. Those three carry INCLUDE columns in the database
    /// that are not modelled here; INCLUDE is a storage detail EF never validates at runtime.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Auth: read on every single authenticated request ---------------------------------
        // (UserSessions.SessionId and DirectoryUsers.Email are primary keys, so those two lookups
        // already seek; the role list was the one that scanned.)
        modelBuilder.Entity<DirectoryUserRoleEntity>()
            .HasIndex(row => row.DirectoryUserEmail)
            .HasDatabaseName("IX_DirectoryUserRoles_DirectoryUserEmail");

        // ---- Requests / RFIs -------------------------------------------------------------------
        modelBuilder.Entity<RequestEntity>()
            .HasIndex(row => new { row.ProjectId, row.Status })
            .HasDatabaseName("IX_Requests_ProjectId_Status");
        modelBuilder.Entity<RequestEntity>()
            .HasIndex(row => new { row.Kind, row.Status })
            .HasDatabaseName("IX_Requests_Kind_Status");
        modelBuilder.Entity<RequestMessageEntity>()
            .HasIndex(row => row.RequestId)
            .HasDatabaseName("IX_RequestMessages_RequestId");

        // ---- Agent activity log ----------------------------------------------------------------
        // Read newest-first, and the filter that matters most is "only what ran unattended".
        modelBuilder.Entity<AgentActivityEntity>()
            .HasIndex(row => row.OccurredAt)
            .HasDatabaseName("IX_AgentActivity_OccurredAt");
        modelBuilder.Entity<AgentActivityEntity>()
            .HasIndex(row => new { row.IsAutonomous, row.OccurredAt })
            .HasDatabaseName("IX_AgentActivity_IsAutonomous_OccurredAt");
        modelBuilder.Entity<AgentActivityEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_AgentActivity_ProjectId");

        // ---- AI connector (OAuth) --------------------------------------------------------------
        // "This user's connected tools" — the profile list and the revoke sweep both start from
        // the user; expired-row cleanup scans by expiry.
        modelBuilder.Entity<OAuthTokenEntity>()
            .HasIndex(row => new { row.UserEmail, row.Kind })
            .HasDatabaseName("IX_OAuthTokens_UserEmail_Kind");
        modelBuilder.Entity<OAuthTokenEntity>()
            .HasIndex(row => row.FamilyId)
            .HasDatabaseName("IX_OAuthTokens_FamilyId");
        modelBuilder.Entity<OAuthAuthCodeEntity>()
            .HasIndex(row => row.ExpiresAt)
            .HasDatabaseName("IX_OAuthAuthCodes_ExpiresAt");

        // ---- Assistant skills --------------------------------------------------------------------
        // Every turn loads the agent's skills plus the shared set, so the agent-key filter is the
        // hot path. RefKey is unique per skill — the model asks for a reference by that pair.
        modelBuilder.Entity<SkillEntity>()
            .HasIndex(row => new { row.AgentKey, row.IsActive })
            .HasDatabaseName("IX_Skills_AgentKey_IsActive");
        modelBuilder.Entity<SkillReferenceEntity>()
            .HasIndex(row => new { row.SkillKey, row.RefKey })
            .IsUnique()
            .HasDatabaseName("IX_SkillReferences_SkillKey_RefKey");
        modelBuilder.Entity<SkillRevisionEntity>()
            .HasIndex(row => new { row.SkillKey, row.Version })
            .HasDatabaseName("IX_SkillRevisions_SkillKey_Version");

        // Unique: a skill is attached to a target once — the admin page's picker saves a target's
        // whole set, so a duplicate edge could only be a bug.
        modelBuilder.Entity<AiActionSkillEntity>()
            .HasIndex(row => new { row.TargetKind, row.TargetKey, row.SkillKey })
            .IsUnique()
            .HasDatabaseName("IX_AiActionSkills_Target_Skill");

        // ---- Company directory (Xero links + contacts) -----------------------------------------
        // Unique: one Xero supplier can only ever be imported once — consolidation re-points the
        // link rather than duplicating it.
        modelBuilder.Entity<SubcontractorXeroLinkEntity>()
            .HasIndex(row => row.XeroContactId)
            .IsUnique()
            .HasDatabaseName("IX_SubcontractorXeroLinks_XeroContactId");
        modelBuilder.Entity<SubcontractorXeroLinkEntity>()
            .HasIndex(row => row.SubcontractorId)
            .HasDatabaseName("IX_SubcontractorXeroLinks_SubcontractorId");
        modelBuilder.Entity<CompanyContactEntity>()
            .HasIndex(row => row.SubcontractorId)
            .HasDatabaseName("IX_CompanyContacts_SubcontractorId");

        // ---- Project contracts -----------------------------------------------------------------
        // Unique: one contract per project. The handlers treat the row as an upsert, and this index
        // is what stops two concurrent first-saves from both inserting.
        modelBuilder.Entity<ProjectContractEntity>()
            .HasIndex(row => row.ProjectId)
            .IsUnique()
            .HasDatabaseName("IX_ProjectContracts_ProjectId");
        // Amendments are always read per project. NOT unique — they accumulate.
        modelBuilder.Entity<ProjectContractAmendmentEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_ProjectContractAmendments_ProjectId");

        // ---- Variations ------------------------------------------------------------------------
        modelBuilder.Entity<VariationOrderEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_VariationOrderQuotes_ProjectId");
        modelBuilder.Entity<VariationOrderEntity>()
            .HasIndex(row => row.RequestId)
            .HasDatabaseName("IX_VariationOrderQuotes_RequestId");
        modelBuilder.Entity<VariationOrderMessageEntity>()
            .HasIndex(row => row.VariationOrderId)
            .HasDatabaseName("IX_VariationOrderMessages_VariationOrderId");

        // ---- Procurement -----------------------------------------------------------------------
        modelBuilder.Entity<WorkOrderEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_WorkOrders_ProjectId");
        modelBuilder.Entity<WorkOrderEntity>()
            .HasIndex(row => row.VariationOrderId)
            .HasDatabaseName("IX_WorkOrders_VariationOrderId");
        modelBuilder.Entity<WorkOrderLineEntity>()
            .HasIndex(row => row.WorkOrderId)
            .HasDatabaseName("IX_WorkOrderLines_WorkOrderId");
        modelBuilder.Entity<BidPackageEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_BidPackages_ProjectId");
        modelBuilder.Entity<BidPackageEntity>()
            .HasIndex(row => row.VariationOrderId)
            .HasDatabaseName("IX_BidPackages_VariationOrderQuoteId");
        modelBuilder.Entity<QuoteEntity>()
            .HasIndex(row => row.BidPackageId)
            .HasDatabaseName("IX_Quotes_BidPackageId");

        // ---- Labour / financials (the first two already live in production) ---------------------
        modelBuilder.Entity<TimesheetEntity>()
            .HasIndex(row => new { row.ProjectId, row.Status })
            .HasDatabaseName("IX_Timesheets_ProjectId_Status");
        modelBuilder.Entity<XeroLineTimesheetCoverEntity>()
            .HasIndex(row => row.XeroLedgerLineId)
            .HasDatabaseName("IX_XeroLineTimesheetCovers_XeroLedgerLineId");
        modelBuilder.Entity<XeroDisputeMessageEntity>()
            .HasIndex(row => row.XeroLedgerLineId)
            .HasDatabaseName("IX_XeroDisputeMessages_XeroLedgerLineId");
        modelBuilder.Entity<WorkOrderBillApprovalEntity>()
            .HasIndex(row => row.XeroInvoiceId)
            .HasDatabaseName("IX_WorkOrderBillApprovals_XeroInvoiceId");
        modelBuilder.Entity<SiteAttendanceEntity>()
            .HasIndex(row => new { row.ProjectId, row.WorkDate })
            .HasDatabaseName("IX_SiteAttendances_ProjectId_WorkDate");
        modelBuilder.Entity<WorkerAbsenceEntity>()
            .HasIndex(row => new { row.WorkerId, row.Date })
            .IsUnique()
            .HasDatabaseName("IX_WorkerAbsences_WorkerId_Date");
        modelBuilder.Entity<LabourWeekSignOffEntity>()
            .HasIndex(row => new { row.WorkerId, row.WeekStart, row.MonthStart })
            .IsUnique()
            .HasDatabaseName("IX_LabourWeekSignOffs_WorkerId_WeekStart_MonthStart");
        modelBuilder.Entity<LabourChaseDismissalEntity>()
            .HasIndex(row => new { row.WorkerId, row.Date })
            .IsUnique()
            .HasDatabaseName("IX_LabourChaseDismissals_WorkerId_Date");
        modelBuilder.Entity<WorkerContractEntity>()
            .HasIndex(row => row.WorkerId)
            .HasDatabaseName("IX_WorkerContracts_WorkerId");
        modelBuilder.Entity<WorkerCisStatusEntity>()
            .HasIndex(row => row.WorkerId)
            .HasDatabaseName("IX_WorkerCisStatuses_WorkerId");
        modelBuilder.Entity<WorkerSettlementLineEntity>()
            .HasIndex(row => new { row.WorkerId, row.Month })
            .HasDatabaseName("IX_WorkerSettlementLines_WorkerId_Month");
        modelBuilder.Entity<SiteXeroMappingEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_SiteXeroMappings_ProjectId");
        modelBuilder.Entity<CostCodeXeroMappingEntity>()
            .HasIndex(row => row.CostCode)
            .HasDatabaseName("IX_CostCodeXeroMappings_CostCode");
        modelBuilder.Entity<XeroCodingRunEntity>()
            .HasIndex(row => new { row.WorkerId, row.Month })
            .HasDatabaseName("IX_XeroCodingRuns_WorkerId_Month");
        modelBuilder.Entity<CompanyRegisterItemEntity>()
            .HasIndex(row => row.Kind)
            .HasDatabaseName("IX_CompanyRegisterItems_Kind");
        modelBuilder.Entity<PolicySignOffEntity>()
            .HasIndex(row => new { row.PolicyDocumentId, row.RecipientEmail })
            .IsUnique()
            .HasDatabaseName("IX_PolicySignOffs_PolicyDocumentId_RecipientEmail");

        // ---- Project-scoped registers -----------------------------------------------------------
        modelBuilder.Entity<DrawingEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_Drawings_ProjectId");
        modelBuilder.Entity<DrawingRevisionEntity>()
            .HasIndex(row => row.DrawingId)
            .HasDatabaseName("IX_DrawingRevisions_DrawingId");
        modelBuilder.Entity<DrawingFolderEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_DrawingFolders_ProjectId");
        modelBuilder.Entity<DrawingFolderEntity>()
            .HasIndex(row => row.ParentDrawingFolderId)
            .HasDatabaseName("IX_DrawingFolders_ParentDrawingFolderId");
        modelBuilder.Entity<HsRecordEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_HsRecords_ProjectId");
        modelBuilder.Entity<TodoItemEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_TodoItems_ProjectId");
        // The record page's "to-dos about this record" read (ListTodoItemsAboutRecord).
        modelBuilder.Entity<TodoItemEntity>()
            .HasIndex(row => row.AboutRecordId)
            .HasDatabaseName("IX_TodoItems_AboutRecordId");
        modelBuilder.Entity<UsefulInformationNoteEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_UsefulInformationNotes_ProjectId");
        // Unique on the canonically-ordered pair: the same two items can only be linked once, and
        // "everything linked to X" seeks this index for the A side and the one below for the B side.
        modelBuilder.Entity<TodoItemLinkEntity>()
            .HasIndex(row => new { row.TodoItemAId, row.TodoItemBId })
            .IsUnique()
            .HasDatabaseName("IX_TodoItemLinks_TodoItemAId_TodoItemBId");
        modelBuilder.Entity<TodoItemLinkEntity>()
            .HasIndex(row => row.TodoItemBId)
            .HasDatabaseName("IX_TodoItemLinks_TodoItemBId");
        modelBuilder.Entity<TodoItemActivityEntity>()
            .HasIndex(row => row.TodoItemId)
            .HasDatabaseName("IX_TodoItemActivities_TodoItemId");
        modelBuilder.Entity<CalendarEventEntity>()
            .HasIndex(row => new { row.ProjectId, row.Date })
            .HasDatabaseName("IX_CalendarEvents_ProjectId_Date");
        modelBuilder.Entity<CalendarEventEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_CalendarEvents_Number");
        // One client reference per cost centre per project.
        modelBuilder.Entity<ClientCostReferenceEntity>()
            .HasIndex(row => new { row.ProjectId, row.CostCode })
            .IsUnique()
            .HasDatabaseName("IX_ClientCostReferences_ProjectId_CostCode");
        modelBuilder.Entity<DefectEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_Defects_ProjectId");
        // Read per project (the Inventory tab's one view); Number resolves INV-#### tags back to
        // their items.
        modelBuilder.Entity<InventoryItemEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_InventoryItems_ProjectId");
        modelBuilder.Entity<InventoryItemEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_InventoryItems_Number");
        // Read per project (the Site Instructions page's one view); Number resolves SI-#### tags
        // back to their instructions.
        modelBuilder.Entity<SiteInstructionEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_SiteInstructions_ProjectId");
        modelBuilder.Entity<SiteInstructionEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_SiteInstructions_Number");

        // ---- Sales (2026-09-06) ----------------------------------------------------------------
        // Leads read as one register, filtered by strategy (the strategy page's funnel) and stage;
        // Number resolves LD-#### references. A lead's timeline reads per lead.
        modelBuilder.Entity<LeadEntity>()
            .HasIndex(row => row.StrategyId)
            .HasDatabaseName("IX_Leads_StrategyId");
        modelBuilder.Entity<LeadEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_Leads_Number");
        modelBuilder.Entity<LeadActivityEntity>()
            .HasIndex(row => row.LeadId)
            .HasDatabaseName("IX_LeadActivities_LeadId");
        modelBuilder.Entity<SalesStrategyEntity>()
            .HasIndex(row => row.Status)
            .HasDatabaseName("IX_SalesStrategies_Status");
        // Imagine (2026-09-06): the public page resolves a lead by its token (unique, filtered so
        // the many nulls don't collide); rounds and images read per lead and per round.
        modelBuilder.Entity<LeadEntity>()
            .HasIndex(row => row.ImagineToken)
            .IsUnique()
            .HasFilter("[ImagineToken] IS NOT NULL")
            .HasDatabaseName("IX_Leads_ImagineToken");
        modelBuilder.Entity<ImagineRoundEntity>()
            .HasIndex(row => row.LeadId)
            .HasDatabaseName("IX_ImagineRounds_LeadId");
        modelBuilder.Entity<ImagineRoundEntity>()
            .HasIndex(row => row.RequestedAt)
            .HasDatabaseName("IX_ImagineRounds_RequestedAt");
        modelBuilder.Entity<ImagineImageEntity>()
            .HasIndex(row => row.LeadId)
            .HasDatabaseName("IX_ImagineImages_LeadId");
        modelBuilder.Entity<ImagineImageEntity>()
            .HasIndex(row => row.RoundId)
            .HasDatabaseName("IX_ImagineImages_RoundId");
        modelBuilder.Entity<SalesProposalEntity>()
            .HasIndex(row => row.LeadId)
            .HasDatabaseName("IX_SalesProposals_LeadId");

        // ---- Draft programme updates (2026-09-08) ---------------------------------------------
        // Mappings read per project (the draft capture and the programme detail) and are replaced
        // per task when a draft is applied; the open draft is found by project + status; lines
        // read per draft.
        modelBuilder.Entity<ProgrammeTaskCostCentreEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_ProgrammeTaskCostCentres_ProjectId");
        modelBuilder.Entity<ProgrammeTaskCostCentreEntity>()
            .HasIndex(row => row.ProgrammeTaskId)
            .HasDatabaseName("IX_ProgrammeTaskCostCentres_ProgrammeTaskId");
        modelBuilder.Entity<ProgrammeDraftEntity>()
            .HasIndex(row => new { row.ProjectId, row.Status })
            .HasDatabaseName("IX_ProgrammeDrafts_ProjectId_Status");
        modelBuilder.Entity<ProgrammeDraftLineEntity>()
            .HasIndex(row => row.ProgrammeDraftId)
            .HasDatabaseName("IX_ProgrammeDraftLines_ProgrammeDraftId");

        // ---- KPI emails --------------------------------------------------------------------------
        // People resolve by portal email (a user's KpiPerson is found, never duplicated); emails
        // read per person (the admin register's filter); Number resolves KPI-#### references; the
        // internet message id answers "is this email already marked for this person".
        modelBuilder.Entity<KpiPersonEntity>()
            .HasIndex(row => row.Email)
            .HasDatabaseName("IX_KpiPeople_Email");
        modelBuilder.Entity<KpiEmailEntity>()
            .HasIndex(row => row.PersonId)
            .HasDatabaseName("IX_KpiEmails_PersonId");
        modelBuilder.Entity<KpiEmailEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_KpiEmails_Number");
        modelBuilder.Entity<KpiEmailEntity>()
            .HasIndex(row => row.InternetMessageId)
            .HasDatabaseName("IX_KpiEmails_InternetMessageId");

        // ---- Building control ---------------------------------------------------------------------
        // Read per project (the tab's one view); numbers resolve BC-####/BCI-#### tags back to
        // their records; attachments are read per case and per inspection.
        modelBuilder.Entity<BuildingControlCaseEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_BuildingControlCases_ProjectId");
        modelBuilder.Entity<BuildingControlCaseEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_BuildingControlCases_Number");
        modelBuilder.Entity<BuildingControlInspectionEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_BuildingControlInspections_ProjectId");
        modelBuilder.Entity<BuildingControlInspectionEntity>()
            .HasIndex(row => row.BuildingControlCaseId)
            .HasDatabaseName("IX_BuildingControlInspections_BuildingControlCaseId");
        modelBuilder.Entity<BuildingControlInspectionEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_BuildingControlInspections_Number");
        modelBuilder.Entity<BuildingControlAttachmentEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_BuildingControlAttachments_ProjectId");
        modelBuilder.Entity<BuildingControlAttachmentEntity>()
            .HasIndex(row => row.BuildingControlCaseId)
            .HasDatabaseName("IX_BuildingControlAttachments_BuildingControlCaseId");
        modelBuilder.Entity<BuildingControlAttachmentEntity>()
            .HasIndex(row => row.BuildingControlInspectionId)
            .HasDatabaseName("IX_BuildingControlAttachments_BuildingControlInspectionId");
        modelBuilder.Entity<ComplianceDocumentEntity>()
            .HasIndex(row => row.SubcontractorId)
            .HasDatabaseName("IX_ComplianceDocuments_SubcontractorId");

        // ---- Architect's Instructions ------------------------------------------------------------
        modelBuilder.Entity<ArchitectInstructionEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_ArchitectInstructions_ProjectId");
        modelBuilder.Entity<ArchitectInstructionVariationEntity>()
            .HasIndex(row => row.ArchitectInstructionId)
            .HasDatabaseName("IX_ArchitectInstructionVariations_ArchitectInstructionId");
        modelBuilder.Entity<ArchitectInstructionVariationEntity>()
            .HasIndex(row => row.VariationOrderId)
            .HasDatabaseName("IX_ArchitectInstructionVariations_VariationOrderId");

        // ---- Request attachments ------------------------------------------------------------------
        modelBuilder.Entity<RequestAttachmentEntity>()
            .HasIndex(row => row.RequestId)
            .HasDatabaseName("IX_RequestAttachments_RequestId");
        // Work-order attachments are always read per order (the PO page's panel and the
        // create-form uploads land through the same list).
        modelBuilder.Entity<WorkOrderAttachmentEntity>()
            .HasIndex(row => row.WorkOrderId)
            .HasDatabaseName("IX_WorkOrderAttachments_WorkOrderId");
        // Bid-package attachments are read per package (the Documents section and the invite draft).
        modelBuilder.Entity<BidPackageAttachmentEntity>()
            .HasIndex(row => row.BidPackageId)
            .HasDatabaseName("IX_BidPackageAttachments_BidPackageId");
        // Email verdicts (Discarded / Extracted) are read per package and joined to the live tagged
        // list; the internet-id index lets a verdict re-find its email after a Graph id change.
        modelBuilder.Entity<BidPackageEmailDispositionEntity>()
            .HasIndex(row => row.BidPackageId)
            .HasDatabaseName("IX_BidPackageEmailDispositions_BidPackageId");
        modelBuilder.Entity<BidPackageEmailDispositionEntity>()
            .HasIndex(row => row.InternetMessageId)
            .HasDatabaseName("IX_BidPackageEmailDispositions_InternetMessageId");

        // ---- Tender enquiries -----------------------------------------------------------------------
        // Read per project (the Tender Enquiries tab) and by number (the TEQ-#### tag resolves back
        // to its record); answers and attachments are read per enquiry.
        modelBuilder.Entity<TenderEnquiryEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_TenderEnquiries_ProjectId");
        modelBuilder.Entity<TenderEnquiryEntity>()
            .HasIndex(row => row.Number)
            .HasDatabaseName("IX_TenderEnquiries_Number");
        modelBuilder.Entity<TenderEnquiryAnswerEntity>()
            .HasIndex(row => row.TenderEnquiryId)
            .HasDatabaseName("IX_TenderEnquiryAnswers_TenderEnquiryId");
        modelBuilder.Entity<TenderEnquiryAttachmentEntity>()
            .HasIndex(row => row.TenderEnquiryId)
            .HasDatabaseName("IX_TenderEnquiryAttachments_TenderEnquiryId");

        // ---- Audit trail ---------------------------------------------------------------------------
        // The register is read per record (a request's own History panel) as well as per project.
        modelBuilder.Entity<AuditEventEntity>()
            .HasIndex(row => row.RecordId)
            .HasDatabaseName("IX_AuditEvents_RecordId");

        // ---- Site P&L ------------------------------------------------------------------------------
        modelBuilder.Entity<XeroSitePnlMonthEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_XeroSitePnlMonths_ProjectId");

        // ---- Document Control ----------------------------------------------------------------------
        // The send handler's "already sent?" read seeks on MessageId. (No UNIQUE composite over
        // MessageId+AttachmentId: two nvarchar(512) Graph ids overshoot SQL Server's 1700-byte
        // index-key cap, so one-row-per-attachment is enforced in the handler instead.)
        modelBuilder.Entity<DocumentControlItemEntity>()
            .HasIndex(row => row.MessageId)
            .HasDatabaseName("IX_DocumentControlItems_MessageId");
        modelBuilder.Entity<DocumentControlItemEntity>()
            .HasIndex(row => row.Status)
            .HasDatabaseName("IX_DocumentControlItems_Status");
        modelBuilder.Entity<PaymentCertificateEntity>()
            .HasIndex(row => row.ProjectId)
            .HasDatabaseName("IX_PaymentCertificates_ProjectId");
        modelBuilder.Entity<DocumentControlItemEntity>()
            .HasIndex(row => row.SourceDocumentControlItemId)
            .HasDatabaseName("IX_DocumentControlItems_SourceDocumentControlItemId");

        // ---- Bluebeam extraction -------------------------------------------------------------------
        // One extraction row per revision (re-extraction overwrites in place); the register's bulk
        // "extract all unprocessed" read scans by project and status.
        modelBuilder.Entity<DrawingExtractionEntity>()
            .HasIndex(row => row.DrawingRevisionId)
            .IsUnique()
            .HasDatabaseName("IX_DrawingExtractions_DrawingRevisionId");
        modelBuilder.Entity<DrawingExtractionEntity>()
            .HasIndex(row => new { row.ProjectId, row.Status })
            .HasDatabaseName("IX_DrawingExtractions_ProjectId_Status");
        modelBuilder.Entity<DrawingMarkupEntity>()
            .HasIndex(row => row.DrawingRevisionId)
            .HasDatabaseName("IX_DrawingMarkups_DrawingRevisionId");
        modelBuilder.Entity<DrawingMarkupEntity>()
            .HasIndex(row => row.DrawingExtractionId)
            .HasDatabaseName("IX_DrawingMarkups_DrawingExtractionId");

        // ---- Valuation % complete: wider than the decimal(18,4) convention ----------------------
        // A line's % is whatever reproduces its claimed value (% x line amount). Four decimal
        // places clip that: 33.3333% of £850,000 is £283,333.05 against the £283,333.33 the QS
        // worked back from, and the report was out by pennies. 20 decimal places keep every figure
        // a user can type or derive, and 28 digits total is exactly what a .NET decimal round-trips
        // without loss (8 integer digits comfortably cover the +/-100000 typo rail). The frozen
        // snapshot copy matches so a submitted report reproduces the live one to the penny.
        // Widened by the WidenClaimPercentPrecision migration (2026-09-02).
        modelBuilder.Entity<ClaimLineEntity>()
            .Property(row => row.PercentComplete)
            .HasPrecision(28, 20);
        modelBuilder.Entity<ValuationReportSnapshotLineEntity>()
            .Property(row => row.PercentComplete)
            .HasPrecision(28, 20);
    }
}
