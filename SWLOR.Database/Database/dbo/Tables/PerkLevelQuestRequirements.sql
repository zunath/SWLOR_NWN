CREATE TABLE [dbo].[PerkLevelQuestRequirements] (
    [PerkLevelQuestRequirementID] INT IDENTITY (1, 1) NOT NULL,
    [PerkLevelID]                 INT NOT NULL,
    [RequiredQuestID]             INT NOT NULL,
    PRIMARY KEY CLUSTERED ([PerkLevelQuestRequirementID] ASC),
    CONSTRAINT [FK_PerkLevelQuestRequirements_PerkLevelID] FOREIGN KEY ([PerkLevelID]) REFERENCES [dbo].[PerkLevels] ([PerkLevelID]),
    CONSTRAINT [FK_PerkLevelQuestRequirements_RequiredQuestID] FOREIGN KEY ([RequiredQuestID]) REFERENCES [dbo].[Quests] ([QuestID])
);

