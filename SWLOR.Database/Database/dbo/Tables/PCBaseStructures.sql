CREATE TABLE [dbo].[PCBaseStructures] (
    [PCBaseStructureID]       INT           IDENTITY (1, 1) NOT NULL,
    [PCBaseID]                INT           NOT NULL,
    [BaseStructureID]         INT           NOT NULL,
    [LocationX]               FLOAT (53)    NOT NULL,
    [LocationY]               FLOAT (53)    NOT NULL,
    [LocationZ]               FLOAT (53)    NOT NULL,
    [LocationOrientation]     FLOAT (53)    NOT NULL,
    [Durability]              FLOAT (53)    DEFAULT ((0.0)) NOT NULL,
    [InteriorStyleID]         INT           NULL,
    [ExteriorStyleID]         INT           NULL,
    [ParentPCBaseStructureID] INT           NULL,
    [CustomName]              NVARCHAR (64) DEFAULT ('') NOT NULL,
    [StructureBonus]          INT           DEFAULT ((0)) NOT NULL,
    [DateNextActivity]        DATETIME2 (7) NULL,
    PRIMARY KEY CLUSTERED ([PCBaseStructureID] ASC),
    CONSTRAINT [FK_PCBaseStructures_BaseStructureID] FOREIGN KEY ([BaseStructureID]) REFERENCES [dbo].[BaseStructures] ([BaseStructureID]),
    CONSTRAINT [FK_PCBaseStructures_ExteriorStyleID] FOREIGN KEY ([ExteriorStyleID]) REFERENCES [dbo].[BuildingStyles] ([BuildingStyleID]),
    CONSTRAINT [FK_PCBaseStructures_InteriorStyleID] FOREIGN KEY ([InteriorStyleID]) REFERENCES [dbo].[BuildingStyles] ([BuildingStyleID]),
    CONSTRAINT [FK_PCBaseStructures_ParentPCBaseStructureID] FOREIGN KEY ([ParentPCBaseStructureID]) REFERENCES [dbo].[PCBaseStructures] ([PCBaseStructureID]),
    CONSTRAINT [FK_PCBaseStructures_PCBaseID] FOREIGN KEY ([PCBaseID]) REFERENCES [dbo].[PCBases] ([PCBaseID])
);

