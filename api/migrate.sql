BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908120000_AddWorkerToXeroLineTimesheetCovers'
)
BEGIN
    ALTER TABLE [XeroLineTimesheetCovers] ADD [WorkerId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908120000_AddWorkerToXeroLineTimesheetCovers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908120000_AddWorkerToXeroLineTimesheetCovers', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908150000_AddProgrammeDrafts'
)
BEGIN
    CREATE TABLE [ProgrammeTaskCostCentres] (
        [ProgrammeTaskCostCentreId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [ProgrammeTaskId] nvarchar(64) NOT NULL,
        [CostCode] nvarchar(32) NOT NULL,
        CONSTRAINT [PK_ProgrammeTaskCostCentres] PRIMARY KEY ([ProgrammeTaskCostCentreId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908150000_AddProgrammeDrafts'
)
BEGIN
    CREATE INDEX [IX_ProgrammeTaskCostCentres_ProjectId] ON [ProgrammeTaskCostCentres] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908150000_AddProgrammeDrafts'
)
BEGIN
    CREATE INDEX [IX_ProgrammeTaskCostCentres_ProgrammeTaskId] ON [ProgrammeTaskCostCentres] ([ProgrammeTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908150000_AddProgrammeDrafts'
)
BEGIN
    CREATE TABLE [ProgrammeDrafts] (
        [ProgrammeDraftId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [ValuationClaimId] nvarchar(64) NOT NULL,
        [ClaimName] nvarchar(128) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [ResolvedAt] datetimeoffset NULL,
        [ResolvedByEmail] nvarchar(256) NOT NULL,
        [SuggestionsRequestedAt] datetimeoffset NULL,
        [SuggestionsNote] nvarchar(512) NOT NULL,
        CONSTRAINT [PK_ProgrammeDrafts] PRIMARY KEY ([ProgrammeDraftId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908150000_AddProgrammeDrafts'
)
BEGIN
    CREATE INDEX [IX_ProgrammeDrafts_ProjectId_Status] ON [ProgrammeDrafts] ([ProjectId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908150000_AddProgrammeDrafts'
)
BEGIN
    CREATE TABLE [ProgrammeDraftLines] (
        [ProgrammeDraftLineId] nvarchar(64) NOT NULL,
        [ProgrammeDraftId] nvarchar(64) NOT NULL,
        [ProgrammeTaskId] nvarchar(64) NOT NULL,
        [TaskTitle] nvarchar(256) NOT NULL,
        [CurrentPercent] decimal(18,4) NOT NULL,
        [ProposedPercent] decimal(18,4) NULL,
        [CostCodes] nvarchar(512) NOT NULL,
        [MappingSource] int NOT NULL,
        [Evidence] nvarchar(1024) NOT NULL,
        [IsIncluded] bit NOT NULL,
        [ReviewedPercent] decimal(18,4) NULL,
        CONSTRAINT [PK_ProgrammeDraftLines] PRIMARY KEY ([ProgrammeDraftLineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908150000_AddProgrammeDrafts'
)
BEGIN
    CREATE INDEX [IX_ProgrammeDraftLines_ProgrammeDraftId] ON [ProgrammeDraftLines] ([ProgrammeDraftId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908150000_AddProgrammeDrafts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908150000_AddProgrammeDrafts', N'8.0.10');
END;
GO

COMMIT;
GO

