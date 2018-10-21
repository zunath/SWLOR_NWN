CREATE TABLE [dbo].[PCBaseStructureItems] (
    [PCBaseStructureItemID] INT            IDENTITY (1, 1) NOT NULL,
    [PCBaseStructureID]     INT            NOT NULL,
    [ItemGlobalID]          NVARCHAR (60)  NOT NULL,
    [ItemName]              NVARCHAR (MAX) NOT NULL,
    [ItemTag]               NVARCHAR (64)  NOT NULL,
    [ItemResref]            NVARCHAR (16)  NOT NULL,
    [ItemObject]            VARCHAR (MAX)  NOT NULL,
    PRIMARY KEY CLUSTERED ([PCBaseStructureItemID] ASC),
    CONSTRAINT [FK_PCBaseStructureItems_PCBaseStructureID] FOREIGN KEY ([PCBaseStructureID]) REFERENCES [dbo].[PCBaseStructures] ([PCBaseStructureID])
);

