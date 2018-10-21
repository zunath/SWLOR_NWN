CREATE TABLE [dbo].[PCKeyItems] (
    [PCKeyItemID]  INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]     NVARCHAR (60) NOT NULL,
    [KeyItemID]    INT           NOT NULL,
    [AcquiredDate] DATETIME2 (0) CONSTRAINT [df_PCKeyItems_AcquiredDate] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK__PCKeyIte__36A246562715831F] PRIMARY KEY CLUSTERED ([PCKeyItemID] ASC),
    CONSTRAINT [fk_PCKeyItems_KeyItemID] FOREIGN KEY ([KeyItemID]) REFERENCES [dbo].[KeyItems] ([KeyItemID]),
    CONSTRAINT [fk_PCKeyItems_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

