CREATE TABLE [dbo].[Skills] (
    [SkillID]               INT             NOT NULL,
    [SkillCategoryID]       INT             NOT NULL,
    [Name]                  NVARCHAR (32)   CONSTRAINT [DF__Skills__Name__3552E9B6] DEFAULT ('') NOT NULL,
    [MaxRank]               INT             CONSTRAINT [DF__Skills__MaxRank__36470DEF] DEFAULT ((0)) NOT NULL,
    [IsActive]              BIT             CONSTRAINT [DF__Skills__IsActive__373B3228] DEFAULT ((0)) NOT NULL,
    [Description]           NVARCHAR (1024) CONSTRAINT [DF__Skills__Descript__382F5661] DEFAULT ('') NOT NULL,
    [Primary]               INT             CONSTRAINT [DF__Skills__Primary__39237A9A] DEFAULT ('') NOT NULL,
    [Secondary]             INT             CONSTRAINT [DF__Skills__Secondar__3A179ED3] DEFAULT ('') NOT NULL,
    [Tertiary]              INT             CONSTRAINT [DF__Skills__Tertiary__3B0BC30C] DEFAULT ('') NOT NULL,
    [ContributesToSkillCap] BIT             DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK__Skills__DFA091E736021CE5] PRIMARY KEY CLUSTERED ([SkillID] ASC),
    CONSTRAINT [FK_Skills_Primary] FOREIGN KEY ([Primary]) REFERENCES [dbo].[Attributes] ([AttributeID]),
    CONSTRAINT [FK_Skills_Secondary] FOREIGN KEY ([Secondary]) REFERENCES [dbo].[Attributes] ([AttributeID]),
    CONSTRAINT [FK_Skills_SkillCategoryID] FOREIGN KEY ([SkillCategoryID]) REFERENCES [dbo].[SkillCategories] ([SkillCategoryID]),
    CONSTRAINT [FK_Skills_Tertiary] FOREIGN KEY ([Tertiary]) REFERENCES [dbo].[Attributes] ([AttributeID])
);

