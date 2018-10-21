CREATE TABLE [dbo].[PCPerks] (
    [PCPerkID]     INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]     NVARCHAR (60) NOT NULL,
    [AcquiredDate] DATETIME2 (7) CONSTRAINT [DF__PCPerks__Acquire__7FEAFD3E] DEFAULT (getutcdate()) NOT NULL,
    [PerkID]       INT           NOT NULL,
    [PerkLevel]    INT           CONSTRAINT [DF__PCPerks__PerkLev__00DF2177] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK__PCPerks__0BA6BCB6B49FBD5D] PRIMARY KEY CLUSTERED ([PCPerkID] ASC),
    CONSTRAINT [fk_PCPerks_PerkID] FOREIGN KEY ([PerkID]) REFERENCES [dbo].[Perks] ([PerkID]),
    CONSTRAINT [fk_PCPerks_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

