CREATE TABLE [dbo].[QuestRewardItems] (
    [QuestRewardItemID] INT           IDENTITY (1, 1) NOT NULL,
    [QuestID]           INT           NOT NULL,
    [Resref]            NVARCHAR (16) NOT NULL,
    [Quantity]          INT           NOT NULL,
    CONSTRAINT [PK_QuestRewards_QuestRewardID] PRIMARY KEY CLUSTERED ([QuestRewardItemID] ASC),
    CONSTRAINT [FK_QuestRewards_QuestID] FOREIGN KEY ([QuestID]) REFERENCES [dbo].[Quests] ([QuestID])
);

