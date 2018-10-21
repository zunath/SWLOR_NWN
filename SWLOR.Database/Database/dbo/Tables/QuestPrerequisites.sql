CREATE TABLE [dbo].[QuestPrerequisites] (
    [QuestPrerequisiteID] INT IDENTITY (1, 1) NOT NULL,
    [QuestID]             INT NOT NULL,
    [RequiredQuestID]     INT NOT NULL,
    CONSTRAINT [PK_QuestPreqrequisites_QuestPrerequisiteID] PRIMARY KEY CLUSTERED ([QuestPrerequisiteID] ASC),
    CONSTRAINT [FK_QuestPrerequisites_QuestID] FOREIGN KEY ([QuestID]) REFERENCES [dbo].[Quests] ([QuestID]),
    CONSTRAINT [FK_QuestPrerequisites_RequiredQuestID] FOREIGN KEY ([RequiredQuestID]) REFERENCES [dbo].[Quests] ([QuestID])
);

