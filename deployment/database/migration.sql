IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE TABLE [Genres] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        CONSTRAINT [PK_Genres] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE TABLE [Movies] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(255) NOT NULL,
        [Slug] nvarchar(255) NOT NULL,
        [OriginalTitle] nvarchar(255) NULL,
        [Description] nvarchar(max) NULL,
        [PosterUrl] nvarchar(500) NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [ReleaseYear] int NULL,
        [Duration] int NULL,
        [Country] nvarchar(100) NULL,
        [Language] nvarchar(100) NULL,
        [MovieType] nvarchar(50) NULL,
        [Status] nvarchar(50) NULL,
        [Quality] nvarchar(50) NULL,
        [Rating] decimal(3,1) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Movies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(255) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE TABLE [Episodes] (
        [Id] int NOT NULL IDENTITY,
        [MovieId] int NOT NULL,
        [EpisodeNumber] int NOT NULL,
        [SeasonNumber] int NOT NULL DEFAULT 1,
        [Title] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NULL,
        [VideoUrl] nvarchar(500) NULL,
        [Duration] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Episodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Episodes_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE TABLE [MovieGenres] (
        [MovieId] int NOT NULL,
        [GenreId] int NOT NULL,
        CONSTRAINT [PK_MovieGenres] PRIMARY KEY ([MovieId], [GenreId]),
        CONSTRAINT [FK_MovieGenres_Genres_GenreId] FOREIGN KEY ([GenreId]) REFERENCES [Genres] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MovieGenres_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(50) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [PasswordHash] nvarchar(255) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RoleId] int NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name', N'Slug') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [Description], [Name], [Slug])
    VALUES (1, N''Currently trending titles'', N''Trending'', N''trending''),
    (2, N''Most watched titles of all time'', N''Popular'', N''popular''),
    (3, N''Curated titles hand-picked by editors'', N''Featured'', N''featured''),
    (4, N''Recently released additions to the platform'', N''Latest'', N''latest'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name', N'Slug') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name', N'Slug') AND [object_id] = OBJECT_ID(N'[Genres]'))
        SET IDENTITY_INSERT [Genres] ON;
    EXEC(N'INSERT INTO [Genres] ([Id], [Description], [Name], [Slug])
    VALUES (1, N''High-energy films featuring physical feats and combat'', N''Action'', N''action''),
    (2, N''Humorous films designed to make audiences laugh'', N''Comedy'', N''comedy''),
    (3, N''Narrative cinema focused on realistic characters and emotional themes'', N''Drama'', N''drama''),
    (4, N''Animated movies created through illustration or 3D rendering'', N''Animation'', N''animation''),
    (5, N''Films designed to elicit fear, suspense, and dread'', N''Horror'', N''horror''),
    (6, N''Speculative fiction based on imagined future scientific or technological advances'', N''Sci-Fi'', N''sci-fi'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name', N'Slug') AND [object_id] = OBJECT_ID(N'[Genres]'))
        SET IDENTITY_INSERT [Genres] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Country', N'CreatedAt', N'Description', N'Duration', N'Language', N'MovieType', N'OriginalTitle', N'PosterUrl', N'Quality', N'Rating', N'ReleaseYear', N'Slug', N'Status', N'ThumbnailUrl', N'Title', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Movies]'))
        SET IDENTITY_INSERT [Movies] ON;
    EXEC(N'INSERT INTO [Movies] ([Id], [Country], [CreatedAt], [Description], [Duration], [Language], [MovieType], [OriginalTitle], [PosterUrl], [Quality], [Rating], [ReleaseYear], [Slug], [Status], [ThumbnailUrl], [Title], [UpdatedAt])
    VALUES (1, N''United States'', ''2025-01-01T00:00:00.0000000Z'', N''A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.'', 148, N''English'', N''Single'', N''Inception'', N''https://example.com/posters/inception.jpg'', N''HD'', 8.8, 2010, N''inception'', N''Completed'', N''https://example.com/thumbnails/inception.jpg'', N''Inception'', ''2025-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Country', N'CreatedAt', N'Description', N'Duration', N'Language', N'MovieType', N'OriginalTitle', N'PosterUrl', N'Quality', N'Rating', N'ReleaseYear', N'Slug', N'Status', N'ThumbnailUrl', N'Title', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Movies]'))
        SET IDENTITY_INSERT [Movies] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([Id], [Description], [Name])
    VALUES (1, N''Standard registered user'', N''User''),
    (2, N''System administrator'', N''Admin'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Description', N'Duration', N'EpisodeNumber', N'MovieId', N'SeasonNumber', N'Title', N'UpdatedAt', N'VideoUrl') AND [object_id] = OBJECT_ID(N'[Episodes]'))
        SET IDENTITY_INSERT [Episodes] ON;
    EXEC(N'INSERT INTO [Episodes] ([Id], [CreatedAt], [Description], [Duration], [EpisodeNumber], [MovieId], [SeasonNumber], [Title], [UpdatedAt], [VideoUrl])
    VALUES (1, ''2025-01-01T00:00:00.0000000Z'', N''Full feature presentation of Inception.'', 148, 1, 1, 1, N''Full Movie'', ''2025-01-01T00:00:00.0000000Z'', N''https://example.com/videos/inception.mp4'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Description', N'Duration', N'EpisodeNumber', N'MovieId', N'SeasonNumber', N'Title', N'UpdatedAt', N'VideoUrl') AND [object_id] = OBJECT_ID(N'[Episodes]'))
        SET IDENTITY_INSERT [Episodes] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'GenreId', N'MovieId') AND [object_id] = OBJECT_ID(N'[MovieGenres]'))
        SET IDENTITY_INSERT [MovieGenres] ON;
    EXEC(N'INSERT INTO [MovieGenres] ([GenreId], [MovieId])
    VALUES (1, 1),
    (6, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'GenreId', N'MovieId') AND [object_id] = OBJECT_ID(N'[MovieGenres]'))
        SET IDENTITY_INSERT [MovieGenres] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Episodes_MovieId] ON [Episodes] ([MovieId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Episodes_MovieId_SeasonNumber_EpisodeNumber] ON [Episodes] ([MovieId], [SeasonNumber], [EpisodeNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Genres_Name] ON [Genres] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Genres_Slug] ON [Genres] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MovieGenres_GenreId] ON [MovieGenres] ([GenreId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Movies_Slug] ON [Movies] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Movies_Title] ON [Movies] ([Title]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928090543_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928090543_InitialCreate', N'8.0.13');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928093006_AddKKPhimIntegrationFields'
)
BEGIN
    ALTER TABLE [Movies] ADD [ExternalId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928093006_AddKKPhimIntegrationFields'
)
BEGIN
    ALTER TABLE [Movies] ADD [ExternalSource] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928093006_AddKKPhimIntegrationFields'
)
BEGIN
    CREATE TABLE [EpisodeSources] (
        [Id] int NOT NULL IDENTITY,
        [EpisodeId] int NOT NULL,
        [ServerName] nvarchar(100) NOT NULL,
        [Language] nvarchar(100) NULL,
        [SourceType] nvarchar(50) NULL,
        [VideoUrl] nvarchar(1000) NOT NULL,
        [EmbedUrl] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EpisodeSources] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EpisodeSources_Episodes_EpisodeId] FOREIGN KEY ([EpisodeId]) REFERENCES [Episodes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928093006_AddKKPhimIntegrationFields'
)
BEGIN
    EXEC(N'UPDATE [Movies] SET [ExternalId] = NULL, [ExternalSource] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928093006_AddKKPhimIntegrationFields'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Movies_ExternalSource_ExternalId] ON [Movies] ([ExternalSource], [ExternalId]) WHERE [ExternalSource] IS NOT NULL AND [ExternalId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928093006_AddKKPhimIntegrationFields'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EpisodeSources_EpisodeId_ServerName_SourceType] ON [EpisodeSources] ([EpisodeId], [ServerName], [SourceType]) WHERE [SourceType] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928093006_AddKKPhimIntegrationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928093006_AddKKPhimIntegrationFields', N'8.0.13');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928095833_AddAuthentication'
)
BEGIN
    DROP INDEX [IX_Users_RoleId] ON [Users];
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'RoleId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
    EXEC(N'UPDATE [Users] SET [RoleId] = 1 WHERE [RoleId] IS NULL');
    ALTER TABLE [Users] ALTER COLUMN [RoleId] int NOT NULL;
    ALTER TABLE [Users] ADD DEFAULT 1 FOR [RoleId];
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928095833_AddAuthentication'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Token] nvarchar(255) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928095833_AddAuthentication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928095833_AddAuthentication'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928095833_AddAuthentication'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928095833_AddAuthentication', N'8.0.13');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928101931_AddMovieSearchIndexes'
)
BEGIN
    CREATE INDEX [IX_Movies_CreatedAt] ON [Movies] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928101931_AddMovieSearchIndexes'
)
BEGIN
    CREATE INDEX [IX_Movies_MovieType] ON [Movies] ([MovieType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928101931_AddMovieSearchIndexes'
)
BEGIN
    CREATE INDEX [IX_Movies_ReleaseYear] ON [Movies] ([ReleaseYear]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928101931_AddMovieSearchIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928101931_AddMovieSearchIndexes', N'8.0.13');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929030930_AddEpisodeDisplayOrderAndSlug'
)
BEGIN
    ALTER TABLE [Episodes] ADD [DisplayOrder] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929030930_AddEpisodeDisplayOrderAndSlug'
)
BEGIN
    ALTER TABLE [Episodes] ADD [Slug] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929030930_AddEpisodeDisplayOrderAndSlug'
)
BEGIN
    EXEC(N'UPDATE [Episodes] SET [DisplayOrder] = 1, [Slug] = N''full-movie''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929030930_AddEpisodeDisplayOrderAndSlug'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929030930_AddEpisodeDisplayOrderAndSlug', N'8.0.13');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE TABLE [Favorites] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [MovieId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Favorites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Favorites_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Favorites_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE TABLE [WatchHistories] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [MovieId] int NOT NULL,
        [EpisodeId] int NOT NULL,
        [ProgressSeconds] int NOT NULL,
        [DurationSeconds] int NULL,
        [LastWatchedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WatchHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WatchHistories_Episodes_EpisodeId] FOREIGN KEY ([EpisodeId]) REFERENCES [Episodes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WatchHistories_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WatchHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE INDEX [IX_Favorites_MovieId] ON [Favorites] ([MovieId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE INDEX [IX_Favorites_UserId_CreatedAt] ON [Favorites] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Favorites_UserId_MovieId] ON [Favorites] ([UserId], [MovieId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE INDEX [IX_WatchHistories_EpisodeId] ON [WatchHistories] ([EpisodeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE INDEX [IX_WatchHistories_MovieId] ON [WatchHistories] ([MovieId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE INDEX [IX_WatchHistories_UserId] ON [WatchHistories] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WatchHistories_UserId_EpisodeId] ON [WatchHistories] ([UserId], [EpisodeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    CREATE INDEX [IX_WatchHistories_UserId_LastWatchedAt] ON [WatchHistories] ([UserId], [LastWatchedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929032829_AddWatchHistoryAndFavorites'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929032829_AddWatchHistoryAndFavorites', N'8.0.13');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929035311_AddMovieRatingsAndComments'
)
BEGIN
    CREATE TABLE [MovieComments] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [MovieId] int NOT NULL,
        [Content] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_MovieComments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MovieComments_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MovieComments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929035311_AddMovieRatingsAndComments'
)
BEGIN
    CREATE TABLE [MovieRatings] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [MovieId] int NOT NULL,
        [Rating] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_MovieRatings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MovieRatings_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MovieRatings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929035311_AddMovieRatingsAndComments'
)
BEGIN
    CREATE INDEX [IX_MovieComments_MovieId_CreatedAt] ON [MovieComments] ([MovieId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929035311_AddMovieRatingsAndComments'
)
BEGIN
    CREATE INDEX [IX_MovieComments_UserId] ON [MovieComments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929035311_AddMovieRatingsAndComments'
)
BEGIN
    CREATE INDEX [IX_MovieRatings_MovieId] ON [MovieRatings] ([MovieId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929035311_AddMovieRatingsAndComments'
)
BEGIN
    CREATE INDEX [IX_MovieRatings_MovieId_Rating] ON [MovieRatings] ([MovieId], [Rating]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929035311_AddMovieRatingsAndComments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MovieRatings_UserId_MovieId] ON [MovieRatings] ([UserId], [MovieId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929035311_AddMovieRatingsAndComments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929035311_AddMovieRatingsAndComments', N'8.0.13');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930100915_AddEnglishAndVietsubFieldsToMovie'
)
BEGIN
    ALTER TABLE [Movies] ADD [IsVietsub] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930100915_AddEnglishAndVietsubFieldsToMovie'
)
BEGIN
    ALTER TABLE [Movies] ADD [LastSyncedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930100915_AddEnglishAndVietsubFieldsToMovie'
)
BEGIN
    ALTER TABLE [Movies] ADD [OriginalLanguage] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930100915_AddEnglishAndVietsubFieldsToMovie'
)
BEGIN
    EXEC(N'UPDATE [Movies] SET [IsVietsub] = CAST(1 AS bit), [LastSyncedAt] = NULL, [OriginalLanguage] = N''English''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930100915_AddEnglishAndVietsubFieldsToMovie'
)
BEGIN
    CREATE INDEX [IX_Movies_Country] ON [Movies] ([Country]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930100915_AddEnglishAndVietsubFieldsToMovie'
)
BEGIN
    CREATE INDEX [IX_Movies_IsVietsub] ON [Movies] ([IsVietsub]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930100915_AddEnglishAndVietsubFieldsToMovie'
)
BEGIN
    CREATE INDEX [IX_Movies_OriginalLanguage] ON [Movies] ([OriginalLanguage]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930100915_AddEnglishAndVietsubFieldsToMovie'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930100915_AddEnglishAndVietsubFieldsToMovie', N'8.0.13');
END;
GO

COMMIT;
GO

