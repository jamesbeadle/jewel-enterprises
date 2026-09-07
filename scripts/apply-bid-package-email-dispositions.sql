-- Applies 20260907160000_AddBidPackageEmailDispositions directly. Identical to what the scoped
-- idempotent EF script would emit for this migration: guarded table + indexes, then the history
-- row. Safe to re-run. Until this runs, "Discard" / "Restore" on a bid package's Submissions tab
-- and the disposition read behind it 500 on the missing table ("Invalid object name
-- 'BidPackageEmailDispositions'"); saving an extracted tender fails the same way.
--
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i apply-bid-package-email-dispositions.sql -b -o apply-bid-package-email-dispositions.log

IF OBJECT_ID(N'[dbo].[BidPackageEmailDispositions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BidPackageEmailDispositions] (
        [BidPackageEmailDispositionId] nvarchar(64)   NOT NULL,
        [BidPackageId]                 nvarchar(64)   NOT NULL,
        -- Graph message id as listed; the stable RFC id re-finds the email if the Graph id changes.
        [MessageId]                    nvarchar(512)  NOT NULL,
        [InternetMessageId]            nvarchar(512)  NULL,
        -- BidPackageEmailOutcome: 1 Discarded (not a tender), 2 Extracted (became QuoteId). Pending is no row.
        [Outcome]                      int            NOT NULL,
        [QuoteId]                      nvarchar(64)   NULL,
        [Note]                         nvarchar(1024) NOT NULL,
        [SetByEmail]                   nvarchar(256)  NOT NULL,
        [SetAt]                        datetimeoffset NOT NULL,
        CONSTRAINT [PK_BidPackageEmailDispositions] PRIMARY KEY ([BidPackageEmailDispositionId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BidPackageEmailDispositions_BidPackageId'
               AND object_id = OBJECT_ID(N'[dbo].[BidPackageEmailDispositions]'))
    CREATE INDEX [IX_BidPackageEmailDispositions_BidPackageId] ON [dbo].[BidPackageEmailDispositions] ([BidPackageId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BidPackageEmailDispositions_InternetMessageId'
               AND object_id = OBJECT_ID(N'[dbo].[BidPackageEmailDispositions]'))
    CREATE INDEX [IX_BidPackageEmailDispositions_InternetMessageId] ON [dbo].[BidPackageEmailDispositions] ([InternetMessageId]);
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260907160000_AddBidPackageEmailDispositions')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907160000_AddBidPackageEmailDispositions', N'8.0.10');
GO
