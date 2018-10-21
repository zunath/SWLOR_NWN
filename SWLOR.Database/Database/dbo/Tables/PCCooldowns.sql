CREATE TABLE [dbo].[PCCooldowns] (
    [PCCooldownID]       INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]           NVARCHAR (60) NOT NULL,
    [CooldownCategoryID] INT           NOT NULL,
    [DateUnlocked]       DATETIME2 (7) NOT NULL,
    CONSTRAINT [PK__PCCooldo__61BCE64547547BE9] PRIMARY KEY CLUSTERED ([PCCooldownID] ASC),
    CONSTRAINT [fk_PCCooldowns_CooldownCategoryID] FOREIGN KEY ([CooldownCategoryID]) REFERENCES [dbo].[CooldownCategories] ([CooldownCategoryID]),
    CONSTRAINT [fk_PCCooldowns_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

