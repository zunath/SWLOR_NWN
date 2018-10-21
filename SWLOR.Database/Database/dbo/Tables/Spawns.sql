CREATE TABLE [dbo].[Spawns] (
    [SpawnID]           INT           NOT NULL,
    [Name]              NVARCHAR (64) NOT NULL,
    [SpawnObjectTypeID] INT           NOT NULL,
    PRIMARY KEY CLUSTERED ([SpawnID] ASC),
    CONSTRAINT [FK_Spawns_SpawnObjectTypeID] FOREIGN KEY ([SpawnObjectTypeID]) REFERENCES [dbo].[SpawnObjectType] ([SpawnObjectTypeID])
);

