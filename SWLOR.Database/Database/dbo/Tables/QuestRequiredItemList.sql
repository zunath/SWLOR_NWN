CREATE TABLE [dbo].[QuestRequiredItemList] (
    [QuestRequiredItemListID] INT           IDENTITY (1, 1) NOT NULL,
    [QuestID]                 INT           NOT NULL,
    [Resref]                  NVARCHAR (16) NOT NULL,
    [Quantity]                INT           NOT NULL,
    [QuestStateID]            INT           NOT NULL,
    [MustBeCraftedByPlayer]   BIT           DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QuestRequiredItemList_QuestRequiredItemListID] PRIMARY KEY CLUSTERED ([QuestRequiredItemListID] ASC),
    CONSTRAINT [FK_QuestRequiredItemList] FOREIGN KEY ([QuestStateID]) REFERENCES [dbo].[QuestStates] ([QuestStateID]),
    CONSTRAINT [FK_QuestRequiredItemList_QuestID] FOREIGN KEY ([QuestID]) REFERENCES [dbo].[Quests] ([QuestID])
);

