CREATE TABLE [dbo].[PCMapProgression] (
    [PCMapProgressionID] INT             IDENTITY (1, 1) NOT NULL,
    [PlayerID]           NVARCHAR (60)   NOT NULL,
    [AreaResref]         NVARCHAR (16)   NOT NULL,
    [Progression]        NVARCHAR (1024) NOT NULL,
    PRIMARY KEY CLUSTERED ([PCMapProgressionID] ASC),
    CONSTRAINT [FK_PCMapProgression_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

