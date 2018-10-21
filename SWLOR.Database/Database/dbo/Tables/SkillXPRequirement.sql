CREATE TABLE [dbo].[SkillXPRequirement] (
    [SkillXPRequirementID] INT IDENTITY (1, 1) NOT NULL,
    [SkillID]              INT NOT NULL,
    [Rank]                 INT NOT NULL,
    [XP]                   INT NOT NULL,
    CONSTRAINT [PK__SkillXPR__A06512642D848122] PRIMARY KEY CLUSTERED ([SkillXPRequirementID] ASC),
    CONSTRAINT [FK_SkillXPRequirement_SkillID] FOREIGN KEY ([SkillID]) REFERENCES [dbo].[Skills] ([SkillID])
);

