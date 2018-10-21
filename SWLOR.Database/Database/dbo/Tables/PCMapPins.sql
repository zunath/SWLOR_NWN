CREATE TABLE [dbo].[PCMapPins] (
    [PCMapPinID] INT             IDENTITY (1, 1) NOT NULL,
    [PlayerID]   NVARCHAR (60)   NOT NULL,
    [AreaTag]    NVARCHAR (32)   NOT NULL,
    [PositionX]  FLOAT (53)      NOT NULL,
    [PositionY]  FLOAT (53)      NOT NULL,
    [NoteText]   NVARCHAR (1024) NOT NULL,
    PRIMARY KEY CLUSTERED ([PCMapPinID] ASC),
    CONSTRAINT [FK_PCMapPins_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

