CREATE TABLE [dbo].[PCMigrationItems] (
    [PCMigrationItemID]   INT           NOT NULL,
    [PCMigrationID]       INT           NOT NULL,
    [CurrentResref]       NVARCHAR (16) NOT NULL,
    [NewResref]           NVARCHAR (16) NOT NULL,
    [StripItemProperties] BIT           NOT NULL,
    [BaseItemTypeID]      INT           NOT NULL,
    CONSTRAINT [PK__PCMigrat__853DDE73AB544BB1] PRIMARY KEY CLUSTERED ([PCMigrationItemID] ASC),
    CONSTRAINT [fk_PCMigrationItems_BaseItemTypeID] FOREIGN KEY ([BaseItemTypeID]) REFERENCES [dbo].[BaseItemTypes] ([BaseItemTypeID]),
    CONSTRAINT [fk_PCMigrationItems_PCMigrationID] FOREIGN KEY ([PCMigrationID]) REFERENCES [dbo].[PCMigrations] ([PCMigrationID])
);

