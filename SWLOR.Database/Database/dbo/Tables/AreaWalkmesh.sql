CREATE TABLE [dbo].[AreaWalkmesh] (
    [AreaWalkmeshID] INT           IDENTITY (1, 1) NOT NULL,
    [AreaID]         NVARCHAR (60) NOT NULL,
    [LocationX]      FLOAT (53)    NULL,
    [LocationY]      FLOAT (53)    NULL,
    [LocationZ]      FLOAT (53)    NOT NULL,
    PRIMARY KEY CLUSTERED ([AreaWalkmeshID] ASC),
    CONSTRAINT [FK_AreaWalkmesh_AreaID] FOREIGN KEY ([AreaID]) REFERENCES [dbo].[Areas] ([AreaID])
);

