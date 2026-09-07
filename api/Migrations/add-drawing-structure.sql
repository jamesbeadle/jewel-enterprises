-- ============================================================================
-- AddDrawingStructure  (2026-09-07)
-- ============================================================================
-- The drawing extraction's own read of the PDF: refs to the positioned-geometry
-- blob and the structured-read blob, the title-block summary (drawing number,
-- revision, scale, whether the sheet proved its scale), the counts a register
-- shows without opening a blob, and MarkupsNote (why Bluebeam markups are
-- absent when they are). Additive only, every column nullable.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260907180000_AddDrawingStructure.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api or worker reads the new columns. Every column guarded on its own.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-drawing-structure.sql -b -o add-drawing-structure.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260907180000_AddDrawingStructure')
BEGIN
    IF COL_LENGTH('DrawingExtractions', 'MarkupsNote') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [MarkupsNote] nvarchar(1024) NULL;
    IF COL_LENGTH('DrawingExtractions', 'GeometryBlobRef') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [GeometryBlobRef] nvarchar(1024) NULL;
    IF COL_LENGTH('DrawingExtractions', 'StructureBlobRef') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [StructureBlobRef] nvarchar(1024) NULL;
    IF COL_LENGTH('DrawingExtractions', 'DimensionCount') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [DimensionCount] int NULL;
    IF COL_LENGTH('DrawingExtractions', 'CalloutCount') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [CalloutCount] int NULL;
    IF COL_LENGTH('DrawingExtractions', 'ShapeCount') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [ShapeCount] int NULL;
    IF COL_LENGTH('DrawingExtractions', 'Scale') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [Scale] nvarchar(32) NULL;
    IF COL_LENGTH('DrawingExtractions', 'ScaleVerified') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [ScaleVerified] bit NULL;
    IF COL_LENGTH('DrawingExtractions', 'DrawingNumber') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [DrawingNumber] nvarchar(128) NULL;
    IF COL_LENGTH('DrawingExtractions', 'RevisionLabel') IS NULL
        ALTER TABLE [DrawingExtractions] ADD [RevisionLabel] nvarchar(32) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260907180000_AddDrawingStructure')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907180000_AddDrawingStructure', N'8.0.10');
END;
GO

COMMIT;
GO
