CREATE TABLE [dbo].[PerkLevelSkillRequirements] (
    [PerkLevelSkillRequirementID] INT IDENTITY (1, 1) NOT NULL,
    [PerkLevelID]                 INT NOT NULL,
    [SkillID]                     INT NOT NULL,
    [RequiredRank]                INT CONSTRAINT [DF__PerkSkill__Requi__278EDA44] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_PerkLevelSkillRequirements_PerkLevelSkillRequirementID] PRIMARY KEY CLUSTERED ([PerkLevelSkillRequirementID] ASC),
    CONSTRAINT [FK_PerkLevelSkillRequirements_PerkLevelID] FOREIGN KEY ([PerkLevelID]) REFERENCES [dbo].[PerkLevels] ([PerkLevelID]),
    CONSTRAINT [FK_PerkLevelSkillRequirements_SkillID] FOREIGN KEY ([SkillID]) REFERENCES [dbo].[Skills] ([SkillID])
);

