
INSERT INTO dbo.LootTables ( LootTableID ,
                             Name )
VALUES ( 44 , -- LootTableID - int
         N'Kath Hounds - Rare Loot' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItems ( LootTableID ,
                                 Resref ,
                                 MaxQuantity ,
                                 Weight ,
                                 IsActive ,
                                 SpawnRule )
VALUES ( 44 ,    -- LootTableID - int
         'k_hound_fur' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItems ( LootTableID ,
                                 Resref ,
                                 MaxQuantity ,
                                 Weight ,
                                 IsActive ,
                                 SpawnRule )
VALUES ( 44 ,    -- LootTableID - int
         'k_hound_tooth' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )