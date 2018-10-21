CREATE TABLE [dbo].[PCSearchSiteItems] (
    [PCSearchSiteItemID] BIGINT        IDENTITY (1, 1) NOT NULL,
    [PlayerID]           NVARCHAR (60) NOT NULL,
    [SearchSiteID]       INT           NOT NULL,
    [SearchItem]         VARCHAR (MAX) NULL,
    CONSTRAINT [PK__PCSearch__001EF3E36436B4F3] PRIMARY KEY CLUSTERED ([PCSearchSiteItemID] ASC),
    CONSTRAINT [fk_PCSearchSiteItems_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

