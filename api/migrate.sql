BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909110000_DropCompanyContactPurpose'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CompanyContacts]') AND [c].[name] = N'Purpose');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [CompanyContacts] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [CompanyContacts] DROP COLUMN [Purpose];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909110000_DropCompanyContactPurpose'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909110000_DropCompanyContactPurpose', N'8.0.10');
END;
GO

COMMIT;
GO

