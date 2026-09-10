-- ============================================================================
-- AddComplianceDocumentPublicLiabilityCover  (2026-09-10)
-- ============================================================================
-- The public liability limit of indemnity a compliance document certifies, in
-- pounds — the accountant's ask: Jewel's insurer requires £5m of every
-- subcontractor on a big job, and the register only said an insurance document
-- existed. One nullable money column; NULL is "not recorded", never nil cover.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260911000000_AddComplianceDocument
-- PublicLiabilityCover.cs. Additive only — safe to apply BEFORE or WITH the
-- deploy; must be applied before the deployed api reads the column.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-compliance-document-public-liability-cover.sql -b \
--          -o add-compliance-document-public-liability-cover.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260911000000_AddComplianceDocumentPublicLiabilityCover')
BEGIN
    IF COL_LENGTH('ComplianceDocuments', 'PublicLiabilityCover') IS NULL
        ALTER TABLE [ComplianceDocuments] ADD [PublicLiabilityCover] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260911000000_AddComplianceDocumentPublicLiabilityCover')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260911000000_AddComplianceDocumentPublicLiabilityCover', N'8.0.10');
END;
GO

COMMIT;
GO
