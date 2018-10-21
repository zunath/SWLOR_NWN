CREATE TABLE [dbo].[QuestStates] (
    [QuestStateID]   INT IDENTITY (1, 1) NOT NULL,
    [QuestID]        INT NOT NULL,
    [Sequence]       INT NOT NULL,
    [QuestTypeID]    INT NOT NULL,
    [JournalStateID] INT NOT NULL,
    CONSTRAINT [PK_QuestStates_QuestStateID] PRIMARY KEY CLUSTERED ([QuestStateID] ASC),
    CONSTRAINT [FK_QuestStates_QuestID] FOREIGN KEY ([QuestID]) REFERENCES [dbo].[Quests] ([QuestID]),
    CONSTRAINT [FK_QuestStates_QuestTypeID] FOREIGN KEY ([QuestTypeID]) REFERENCES [dbo].[QuestTypeDomain] ([QuestTypeID])
);

