CREATE TABLE [dbo].[SkillCategories] (
    [SkillCategoryID] INT           NOT NULL,
    [Name]            NVARCHAR (32) CONSTRAINT [DF__SkillCateg__Name__32767D0B] DEFAULT ('') NOT NULL,
    [IsActive]        BIT           CONSTRAINT [DF__SkillCate__IsAct__336AA144] DEFAULT ((0)) NOT NULL,
    [Sequence]        INT           CONSTRAINT [DF__SkillCate__Seque__345EC57D] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK__SkillCat__D2A5F8BCCC134052] PRIMARY KEY CLUSTERED ([SkillCategoryID] ASC)
);

