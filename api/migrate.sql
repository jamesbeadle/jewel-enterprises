BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910110000_AddValuationInvoiceXeroRaise'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [XeroInvoiceId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910110000_AddValuationInvoiceXeroRaise'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [XeroInvoiceNumber] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910110000_AddValuationInvoiceXeroRaise'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [XeroRaisedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910110000_AddValuationInvoiceXeroRaise'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910110000_AddValuationInvoiceXeroRaise', N'8.0.10');
END;
GO

COMMIT;
GO

