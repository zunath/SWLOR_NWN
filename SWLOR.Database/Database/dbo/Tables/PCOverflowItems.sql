CREATE TABLE [dbo].[PCOverflowItems] (
    [PCOverflowItemID] BIGINT         IDENTITY (1, 1) NOT NULL,
    [PlayerID]         NVARCHAR (60)  NOT NULL,
    [ItemName]         NVARCHAR (MAX) NOT NULL,
    [ItemTag]          NVARCHAR (64)  NOT NULL,
    [ItemResref]       NVARCHAR (16)  NOT NULL,
    [ItemObject]       VARCHAR (MAX)  NULL,
    CONSTRAINT [PK__PCOverfl__F923885539F635E2] PRIMARY KEY CLUSTERED ([PCOverflowItemID] ASC),
    CONSTRAINT [fk_PCOverflowItems_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

