CREATE TABLE [dbo].[PCCraftedBlueprints] (
    [PCCraftedBlueprintID] INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]             NVARCHAR (60) NOT NULL,
    [CraftBlueprintID]     BIGINT        NOT NULL,
    [DateFirstCrafted]     DATETIME2 (7) DEFAULT (getutcdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([PCCraftedBlueprintID] ASC),
    CONSTRAINT [FK_PCCraftedBlueprints_CraftBlueprintID] FOREIGN KEY ([CraftBlueprintID]) REFERENCES [dbo].[CraftBlueprints] ([CraftBlueprintID]),
    CONSTRAINT [FK_PCCraftedBlueprints_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

