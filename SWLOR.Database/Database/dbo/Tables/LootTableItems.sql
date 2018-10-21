CREATE TABLE [dbo].[LootTableItems] (
    [LootTableItemID] INT           IDENTITY (1, 1) NOT NULL,
    [LootTableID]     INT           NOT NULL,
    [Resref]          VARCHAR (16)  NOT NULL,
    [MaxQuantity]     INT           NOT NULL,
    [Weight]          TINYINT       NOT NULL,
    [IsActive]        BIT           NOT NULL,
    [SpawnRule]       NVARCHAR (64) DEFAULT ('') NOT NULL,
    CONSTRAINT [PK__LootTabl__E0F42FED5CB8C330] PRIMARY KEY CLUSTERED ([LootTableItemID] ASC),
    CONSTRAINT [fk_LootTableItems_LootTableID] FOREIGN KEY ([LootTableID]) REFERENCES [dbo].[LootTables] ([LootTableID])
);

