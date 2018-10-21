CREATE TABLE [dbo].[QuestKillTargetList] (
    [QuestKillTargetListID] INT IDENTITY (1, 1) NOT NULL,
    [QuestID]               INT NOT NULL,
    [NPCGroupID]            INT NOT NULL,
    [Quantity]              INT NOT NULL,
    [QuestStateID]          INT NOT NULL,
    CONSTRAINT [PK_QuestKillTargetList_QuestKillTargetListID] PRIMARY KEY CLUSTERED ([QuestKillTargetListID] ASC),
    CONSTRAINT [FK_QuestKillTargetList_NPCGroupID] FOREIGN KEY ([NPCGroupID]) REFERENCES [dbo].[NPCGroups] ([NPCGroupID]),
    CONSTRAINT [FK_QuestKillTargetList_QuestID] FOREIGN KEY ([QuestID]) REFERENCES [dbo].[Quests] ([QuestID]),
    CONSTRAINT [FK_QuestKillTargetList_QuestStateID] FOREIGN KEY ([QuestStateID]) REFERENCES [dbo].[QuestStates] ([QuestStateID])
);

