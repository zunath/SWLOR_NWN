CREATE TABLE [dbo].[BaseStructures] (
    [BaseStructureID]     INT           NOT NULL,
    [BaseStructureTypeID] INT           NOT NULL,
    [Name]                NVARCHAR (64) DEFAULT ('') NOT NULL,
    [PlaceableResref]     NVARCHAR (16) DEFAULT ('') NOT NULL,
    [ItemResref]          NVARCHAR (16) DEFAULT ('') NOT NULL,
    [IsActive]            BIT           DEFAULT ((0)) NOT NULL,
    [Power]               FLOAT (53)    DEFAULT ((0.0)) NOT NULL,
    [CPU]                 FLOAT (53)    DEFAULT ((0.0)) NOT NULL,
    [Durability]          FLOAT (53)    DEFAULT ((0.0)) NOT NULL,
    [Storage]             INT           DEFAULT ((0)) NOT NULL,
    [HasAtmosphere]       BIT           DEFAULT ((0)) NOT NULL,
    [ReinforcedStorage]   INT           DEFAULT ((0)) NOT NULL,
    [RequiresBasePower]   BIT           DEFAULT ((0)) NOT NULL,
    [ResourceStorage]     INT           DEFAULT ((0)) NOT NULL,
    [RetrievalRating]     INT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([BaseStructureID] ASC),
    CONSTRAINT [FK_BaseStructures_BaseStructureTypeID] FOREIGN KEY ([BaseStructureTypeID]) REFERENCES [dbo].[BaseStructureType] ([BaseStructureTypeID])
);

