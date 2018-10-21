CREATE TABLE [dbo].[PCQuestKillTargetProgress] (
    [PCQuestKillTargetProgressID] INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]                    NVARCHAR (60) NOT NULL,
    [PCQuestStatusID]             INT           NOT NULL,
    [NPCGroupID]                  INT           NOT NULL,
    [RemainingToKill]             INT           NOT NULL,
    CONSTRAINT [PK_PCQuestKillTargetProgress_PCQuestKillTargetProgressID] PRIMARY KEY CLUSTERED ([PCQuestKillTargetProgressID] ASC),
    CONSTRAINT [FK_PCQuestKillTargetProgress_NPCGroupID] FOREIGN KEY ([NPCGroupID]) REFERENCES [dbo].[NPCGroups] ([NPCGroupID]),
    CONSTRAINT [FK_PCQuestKillTargetProgress_PCQuestStatusID] FOREIGN KEY ([PCQuestStatusID]) REFERENCES [dbo].[PCQuestStatus] ([PCQuestStatusID]),
    CONSTRAINT [FK_PCQuestKillTargetProgress_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

