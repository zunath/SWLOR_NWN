CREATE TABLE [dbo].[PCImpoundedItems] (
    [PCImpoundedItemID] INT            IDENTITY (1, 1) NOT NULL,
    [PlayerID]          NVARCHAR (60)  NOT NULL,
    [ItemName]          NVARCHAR (64)  NOT NULL,
    [ItemTag]           NVARCHAR (32)  NOT NULL,
    [ItemResref]        NVARCHAR (16)  NOT NULL,
    [ItemObject]        NVARCHAR (MAX) NOT NULL,
    [DateImpounded]     DATETIME2 (7)  DEFAULT (getutcdate()) NOT NULL,
    [DateRetrieved]     DATETIME2 (7)  NULL,
    PRIMARY KEY CLUSTERED ([PCImpoundedItemID] ASC),
    CONSTRAINT [FK_PCItemImpound_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

