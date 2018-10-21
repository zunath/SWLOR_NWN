CREATE TABLE [dbo].[SpawnObjects] (
    [SpawnObjectID]   INT           IDENTITY (1, 1) NOT NULL,
    [SpawnID]         INT           NOT NULL,
    [Resref]          NVARCHAR (16) NOT NULL,
    [Weight]          INT           DEFAULT ((1)) NOT NULL,
    [SpawnRule]       NVARCHAR (32) DEFAULT ('') NOT NULL,
    [NPCGroupID]      INT           NULL,
    [BehaviourScript] NVARCHAR (64) DEFAULT ('') NOT NULL,
    [DeathVFXID]      INT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([SpawnObjectID] ASC),
    CONSTRAINT [FK_SpawnObjects_NPCGroupID] FOREIGN KEY ([NPCGroupID]) REFERENCES [dbo].[NPCGroups] ([NPCGroupID]),
    CONSTRAINT [FK_SpawnObjects_SpawnID] FOREIGN KEY ([SpawnID]) REFERENCES [dbo].[Spawns] ([SpawnID])
);

