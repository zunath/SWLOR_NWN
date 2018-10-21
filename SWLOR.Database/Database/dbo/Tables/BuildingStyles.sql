CREATE TABLE [dbo].[BuildingStyles] (
    [BuildingStyleID] INT           NOT NULL,
    [Name]            NVARCHAR (64) DEFAULT ('') NOT NULL,
    [Resref]          NVARCHAR (16) DEFAULT ('') NOT NULL,
    [BaseStructureID] INT           NULL,
    [IsDefault]       BIT           DEFAULT ((0)) NOT NULL,
    [DoorRule]        NVARCHAR (64) DEFAULT ('') NOT NULL,
    [IsActive]        BIT           DEFAULT ((0)) NOT NULL,
    [BuildingTypeID]  INT           NOT NULL,
    [PurchasePrice]   INT           DEFAULT ((0)) NOT NULL,
    [DailyUpkeep]     INT           DEFAULT ((0)) NOT NULL,
    [FurnitureLimit]  INT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([BuildingStyleID] ASC),
    CONSTRAINT [FK_BuildingStyles_BaseStructureID] FOREIGN KEY ([BaseStructureID]) REFERENCES [dbo].[BaseStructures] ([BaseStructureID]),
    CONSTRAINT [FK_BuildingStyles_BuildingTypeID] FOREIGN KEY ([BuildingTypeID]) REFERENCES [dbo].[BuildingTypes] ([BuildingTypeID])
);

