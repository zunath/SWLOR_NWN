CREATE TABLE [dbo].[PerkCategories] (
    [PerkCategoryID] INT           NOT NULL,
    [Name]           NVARCHAR (64) CONSTRAINT [DF__PerkCatego__Name__078C1F06] DEFAULT ('') NOT NULL,
    [IsActive]       BIT           CONSTRAINT [DF__PerkCateg__IsAct__0880433F] DEFAULT ((0)) NOT NULL,
    [Sequence]       INT           CONSTRAINT [DF__PerkCateg__Seque__09746778] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK__PerkCate__9777DCFC136BDCB4] PRIMARY KEY CLUSTERED ([PerkCategoryID] ASC)
);

