CREATE TABLE [dbo].[PerkLevels] (
    [PerkLevelID] INT            IDENTITY (1, 1) NOT NULL,
    [PerkID]      INT            NOT NULL,
    [Level]       INT            CONSTRAINT [DF__PerkLevel__Level__0B5CAFEA] DEFAULT ((0)) NOT NULL,
    [Price]       INT            CONSTRAINT [DF__PerkLevel__Price__0C50D423] DEFAULT ((0)) NOT NULL,
    [Description] NVARCHAR (512) CONSTRAINT [DF__PerkLevel__Descr__0D44F85C] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK__PerkLeve__A934EB471F92073F] PRIMARY KEY CLUSTERED ([PerkLevelID] ASC),
    CONSTRAINT [FK_PerkLevels_PerkID] FOREIGN KEY ([PerkID]) REFERENCES [dbo].[Perks] ([PerkID]),
    CONSTRAINT [uq_PerkLevels_PerkIDLevel] UNIQUE NONCLUSTERED ([PerkID] ASC, [Level] ASC)
);

