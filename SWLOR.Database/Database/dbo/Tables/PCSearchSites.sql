CREATE TABLE [dbo].[PCSearchSites] (
    [PCSearchSiteID] INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]       NVARCHAR (60) NOT NULL,
    [SearchSiteID]   INT           NOT NULL,
    [UnlockDateTime] DATETIME      NOT NULL,
    CONSTRAINT [PK__PCSearch__B988F45255B968F1] PRIMARY KEY CLUSTERED ([PCSearchSiteID] ASC),
    CONSTRAINT [fk_PCSearchSites_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

