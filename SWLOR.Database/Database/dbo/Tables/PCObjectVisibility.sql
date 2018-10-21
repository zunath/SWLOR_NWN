CREATE TABLE [dbo].[PCObjectVisibility] (
    [PCObjectVisibilityID] INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]             NVARCHAR (60) NOT NULL,
    [VisibilityObjectID]   NVARCHAR (60) NOT NULL,
    [IsVisible]            BIT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([PCObjectVisibilityID] ASC),
    CONSTRAINT [FK_PCObjectVisibility_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

