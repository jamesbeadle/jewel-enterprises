-- ============================================================================
-- AddProjectXeroContact  (2026-09-10)
-- ============================================================================
-- Projects gain XeroContactId (nvarchar(64), nullable) and XeroContactName
-- (nvarchar(256), nullable): the Xero customer the project's sales invoices
-- are raised on, mapped in Project settings beside the Sites option. Raise in
-- Xero is blocked until the id is set — it no longer matches the client by
-- name and never creates a contact (the accountant's ask, after a name match
-- created a duplicate). Additive only, so it is safe to apply BEFORE the
-- deploy. Mirrors api/Migrations/20260910230000_AddProjectXeroContact.cs and
-- records itself in __EFMigrationsHistory so EF never re-applies it.
--
-- Apply:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--     -i scripts/2026-09-10-add-project-xero-contact.sql -b
-- ============================================================================

BEGIN TRANSACTION;
GO

IF COL_LENGTH('dbo.Projects', 'XeroContactId') IS NULL
BEGIN
    ALTER TABLE [Projects] ADD [XeroContactId] nvarchar(64) NULL;
END;
GO

IF COL_LENGTH('dbo.Projects', 'XeroContactName') IS NULL
BEGIN
    ALTER TABLE [Projects] ADD [XeroContactName] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910230000_AddProjectXeroContact'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910230000_AddProjectXeroContact', N'8.0.10');
END;
GO

COMMIT;
GO
