CREATE TABLE [dbo].[ChatChannelsDomain] (
    [ChatChannelID] INT           NOT NULL,
    [Name]          NVARCHAR (64) CONSTRAINT [DF__ChatChanne__Name__58D1301D] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK__ChatChan__7153E07326A4B168] PRIMARY KEY CLUSTERED ([ChatChannelID] ASC)
);

