BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909160000_AddProgrammeVariationEffects'
)
BEGIN
    CREATE TABLE [ProgrammeVariationEffects] (
        [ProgrammeVariationEffectId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [VariationOrderId] nvarchar(64) NOT NULL,
        [ProgrammeTaskId] nvarchar(64) NOT NULL,
        [DelayDays] int NOT NULL,
        [Note] nvarchar(512) NOT NULL,
        [RecordedByEmail] nvarchar(256) NOT NULL,
        [RecordedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ProgrammeVariationEffects] PRIMARY KEY ([ProgrammeVariationEffectId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909160000_AddProgrammeVariationEffects'
)
BEGIN
    CREATE INDEX [IX_ProgrammeVariationEffects_ProjectId] ON [ProgrammeVariationEffects] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909160000_AddProgrammeVariationEffects'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProgrammeVariationEffects_VariationOrderId_ProgrammeTaskId] ON [ProgrammeVariationEffects] ([VariationOrderId], [ProgrammeTaskId]) WHERE [VariationOrderId] IS NOT NULL AND [ProgrammeTaskId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909160000_AddProgrammeVariationEffects'
)
BEGIN
    ALTER TABLE [Requests] ADD [EotDaysClaimed] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909160000_AddProgrammeVariationEffects'
)
BEGIN
    ALTER TABLE [Requests] ADD [EotDaysGranted] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909160000_AddProgrammeVariationEffects'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909160000_AddProgrammeVariationEffects', N'8.0.10');
END;
GO

COMMIT;
GO

