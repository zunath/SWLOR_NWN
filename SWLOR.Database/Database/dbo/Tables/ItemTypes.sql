CREATE TABLE [dbo].[ItemTypes] (
    [ItemTypeID] INT           NOT NULL,
    [Name]       NVARCHAR (32) CONSTRAINT [DF__ItemTypes__Name__7E02B4CC] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK__ItemType__F51540DB3DC6DAE5] PRIMARY KEY CLUSTERED ([ItemTypeID] ASC)
);

