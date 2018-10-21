CREATE TABLE [dbo].[PCOutfits] (
    [PlayerID] NVARCHAR (60) NOT NULL,
    [Outfit1]  VARCHAR (MAX) NULL,
    [Outfit2]  VARCHAR (MAX) NULL,
    [Outfit3]  VARCHAR (MAX) NULL,
    [Outfit4]  VARCHAR (MAX) NULL,
    [Outfit5]  VARCHAR (MAX) NULL,
    [Outfit6]  VARCHAR (MAX) NULL,
    [Outfit7]  VARCHAR (MAX) NULL,
    [Outfit8]  VARCHAR (MAX) NULL,
    [Outfit9]  VARCHAR (MAX) NULL,
    [Outfit10] VARCHAR (MAX) NULL,
    CONSTRAINT [PK__PCOutfit__4A4E74A8B41DD37A] PRIMARY KEY CLUSTERED ([PlayerID] ASC),
    CONSTRAINT [fk_PCOutfits_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

