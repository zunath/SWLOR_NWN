CREATE TABLE [dbo].[CooldownCategories] (
    [CooldownCategoryID] INT           NOT NULL,
    [Name]               NVARCHAR (64) CONSTRAINT [DF__CooldownCa__Name__5F7E2DAC] DEFAULT ('') NOT NULL,
    [BaseCooldownTime]   FLOAT (53)    CONSTRAINT [DF__CooldownC__BaseC__607251E5] DEFAULT ((0.0)) NOT NULL,
    CONSTRAINT [PK__Cooldown__049008DC1A120AC0] PRIMARY KEY CLUSTERED ([CooldownCategoryID] ASC)
);

