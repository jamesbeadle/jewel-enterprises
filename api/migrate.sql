BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910100000_AddDocumentOcrResults'
)
BEGIN
    CREATE TABLE [DocumentOcrResults] (
        [ContentSha256] nvarchar(64) NOT NULL,
        [PageCount] int NOT NULL,
        [PagesJson] nvarchar(max) NOT NULL,
        [Confidence] float NOT NULL,
        [Provider] nvarchar(64) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_DocumentOcrResults] PRIMARY KEY ([ContentSha256])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910100000_AddDocumentOcrResults'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910100000_AddDocumentOcrResults', N'8.0.10');
END;
GO

COMMIT;
GO

