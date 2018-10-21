CREATE TABLE [dbo].[QuestRequiredKeyItemList] (
    [QuestRequiredKeyItemID] INT IDENTITY (1, 1) NOT NULL,
    [QuestID]                INT NOT NULL,
    [KeyItemID]              INT NOT NULL,
    [QuestStateID]           INT NOT NULL,
    CONSTRAINT [PK_QuestRequiredKeyItemList_QuestRequiredKeyItemID] PRIMARY KEY CLUSTERED ([QuestRequiredKeyItemID] ASC),
    CONSTRAINT [FK_QuestRequiredKeyItemList] FOREIGN KEY ([QuestStateID]) REFERENCES [dbo].[QuestStates] ([QuestStateID]),
    CONSTRAINT [FK_QuestRequiredKeyItemList_KeyItemID] FOREIGN KEY ([KeyItemID]) REFERENCES [dbo].[KeyItems] ([KeyItemID]),
    CONSTRAINT [FK_QuestRequiredKeyItemList_QuestID] FOREIGN KEY ([QuestID]) REFERENCES [dbo].[Quests] ([QuestID])
);

