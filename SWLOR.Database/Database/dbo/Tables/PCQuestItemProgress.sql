CREATE TABLE [dbo].[PCQuestItemProgress] (
    [PCQuestItemProgressID] INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]              NVARCHAR (60) NOT NULL,
    [PCQuestStatusID]       INT           NOT NULL,
    [Resref]                NVARCHAR (16) NOT NULL,
    [Remaining]             INT           DEFAULT ((0)) NOT NULL,
    [MustBeCraftedByPlayer] BIT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([PCQuestItemProgressID] ASC),
    CONSTRAINT [FK_PCQuestItemProgress_PCQuestStatusID] FOREIGN KEY ([PCQuestStatusID]) REFERENCES [dbo].[PCQuestStatus] ([PCQuestStatusID]),
    CONSTRAINT [FK_PCQuestItemProgress_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

