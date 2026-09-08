BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908200000_AddXeroLineWriteBackFailedAt'
)
BEGIN
    ALTER TABLE [XeroLedgerLines] ADD [WriteBackFailedAtUtc] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908200000_AddXeroLineWriteBackFailedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908200000_AddXeroLineWriteBackFailedAt', N'8.0.10');
END;
GO

COMMIT;
GO

