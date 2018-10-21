CREATE TABLE [dbo].[PCQuestStatus] (
    [PCQuestStatusID]      INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]             NVARCHAR (60) NOT NULL,
    [QuestID]              INT           NOT NULL,
    [CurrentQuestStateID]  INT           NOT NULL,
    [CompletionDate]       DATETIME2 (7) NULL,
    [SelectedItemRewardID] INT           NULL,
    CONSTRAINT [PK_PCQuestStatus_PCQuestStatusID] PRIMARY KEY CLUSTERED ([PCQuestStatusID] ASC),
    CONSTRAINT [FK_PCQuestStatus_CurrentQuestStateID] FOREIGN KEY ([CurrentQuestStateID]) REFERENCES [dbo].[QuestStates] ([QuestStateID]),
    CONSTRAINT [FK_PCQuestStatus_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID]),
    CONSTRAINT [FK_PCQuestStatus_QuestID] FOREIGN KEY ([QuestID]) REFERENCES [dbo].[Quests] ([QuestID]),
    CONSTRAINT [FK_PCQuestStatus_SelectedRewardID] FOREIGN KEY ([SelectedItemRewardID]) REFERENCES [dbo].[QuestRewardItems] ([QuestRewardItemID])
);

