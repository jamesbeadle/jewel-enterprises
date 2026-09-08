BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908180000_AddWorkOrderBillApprovals'
)
BEGIN
    CREATE TABLE [WorkOrderBillApprovals] (
        [WorkOrderBillApprovalId] nvarchar(64) NOT NULL,
        [XeroInvoiceId] nvarchar(64) NOT NULL,
        [WorkOrderId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [MatchRule] int NOT NULL,
        [MatchDetail] nvarchar(512) NOT NULL,
        [BillNet] decimal(18,4) NOT NULL,
        [ApprovedByEmail] nvarchar(256) NOT NULL,
        [ApprovedAtUtc] datetimeoffset NOT NULL,
        [UndoneByEmail] nvarchar(256) NULL,
        [UndoneAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_WorkOrderBillApprovals] PRIMARY KEY ([WorkOrderBillApprovalId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908180000_AddWorkOrderBillApprovals'
)
BEGIN
    CREATE INDEX [IX_WorkOrderBillApprovals_XeroInvoiceId] ON [WorkOrderBillApprovals] ([XeroInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908180000_AddWorkOrderBillApprovals'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908180000_AddWorkOrderBillApprovals', N'8.0.10');
END;
GO

COMMIT;
GO

