CREATE TABLE [dbo].[PCRegionalFame] (
    [PCRegionalFameID] INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]         NVARCHAR (60) NOT NULL,
    [FameRegionID]     INT           NOT NULL,
    [Amount]           INT           CONSTRAINT [DF__PCRegiona__Amoun__01D345B0] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_PCRegionalFame_PCRegionalFameID] PRIMARY KEY CLUSTERED ([PCRegionalFameID] ASC),
    CONSTRAINT [FK_PCRegionalFame_FameRegionID] FOREIGN KEY ([FameRegionID]) REFERENCES [dbo].[FameRegions] ([FameRegionID]),
    CONSTRAINT [FK_PCRegionalFame_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

