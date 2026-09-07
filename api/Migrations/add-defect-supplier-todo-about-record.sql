-- ============================================================================
-- AddDefectSupplierAndTodoAboutRecord  (2026-09-07)
-- ============================================================================
-- Defects raised WITH a supplier + to-dos ABOUT a record.
--   Defects   + SubcontractorId (directory record the defect is raised with)
--             + SentToSupplierAt / SentToSupplierByEmail (first send from the
--               defect's page — stamped by the compose pipeline)
--   TodoItems + AboutRecordType / AboutRecordId (the record a to-do is about;
--               a defect first, any record type later) + index on the id
--
-- House-style scoped script: additive, every column nullable, so it is safe
-- to apply BEFORE or WITH the deploy. Mirrors
-- api/Migrations/20260907120000_AddDefectSupplierAndTodoAboutRecord.cs and
-- records the id in __EFMigrationsHistory so EF never re-applies it.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-defect-supplier-todo-about-record.sql -b -o add-defect-supplier-todo-about-record.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907120000_AddDefectSupplierAndTodoAboutRecord'
)
BEGIN
    IF COL_LENGTH('Defects', 'SubcontractorId') IS NULL
        ALTER TABLE [Defects] ADD [SubcontractorId] nvarchar(64) NULL;
    IF COL_LENGTH('Defects', 'SentToSupplierAt') IS NULL
        ALTER TABLE [Defects] ADD [SentToSupplierAt] datetimeoffset NULL;
    IF COL_LENGTH('Defects', 'SentToSupplierByEmail') IS NULL
        ALTER TABLE [Defects] ADD [SentToSupplierByEmail] nvarchar(256) NULL;

    IF COL_LENGTH('TodoItems', 'AboutRecordType') IS NULL
        ALTER TABLE [TodoItems] ADD [AboutRecordType] int NULL;
    IF COL_LENGTH('TodoItems', 'AboutRecordId') IS NULL
        ALTER TABLE [TodoItems] ADD [AboutRecordId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TodoItems_AboutRecordId' AND object_id = OBJECT_ID('TodoItems'))
    CREATE INDEX [IX_TodoItems_AboutRecordId] ON [TodoItems] ([AboutRecordId]);
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907120000_AddDefectSupplierAndTodoAboutRecord'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907120000_AddDefectSupplierAndTodoAboutRecord', N'8.0.10');
END;
GO

COMMIT;
GO
