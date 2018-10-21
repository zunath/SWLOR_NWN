CREATE TABLE [dbo].[PCPerkRefunds] (
    [PCPerkRefundID] INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]       NVARCHAR (60) NOT NULL,
    [PerkID]         INT           NOT NULL,
    [Level]          INT           NOT NULL,
    [DateAcquired]   DATETIME2 (7) NOT NULL,
    [DateRefunded]   DATETIME2 (7) DEFAULT (getutcdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([PCPerkRefundID] ASC),
    CONSTRAINT [FK_PCPerkRefunds_PerkID] FOREIGN KEY ([PerkID]) REFERENCES [dbo].[Perks] ([PerkID]),
    CONSTRAINT [FK_PCPerkRefunds_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

