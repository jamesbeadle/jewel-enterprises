BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909150000_AddXeroLineWorkOrderLinkCostCenterCode'
)
BEGIN
    ALTER TABLE [XeroLineWorkOrderLinks] ADD [CostCenterCode] nvarchar(32) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909150000_AddXeroLineWorkOrderLinkCostCenterCode'
)
BEGIN
    DROP INDEX [UX_XeroLineWorkOrderLinks_Line_Order] ON [XeroLineWorkOrderLinks];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909150000_AddXeroLineWorkOrderLinkCostCenterCode'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_XeroLineWorkOrderLinks_Line_Order_Code] ON [XeroLineWorkOrderLinks] ([XeroLedgerLineId], [WorkOrderId], [CostCenterCode]) WHERE [XeroLedgerLineId] IS NOT NULL AND [WorkOrderId] IS NOT NULL AND [CostCenterCode] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909150000_AddXeroLineWorkOrderLinkCostCenterCode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909150000_AddXeroLineWorkOrderLinkCostCenterCode', N'8.0.10');
END;
GO

COMMIT;
GO

