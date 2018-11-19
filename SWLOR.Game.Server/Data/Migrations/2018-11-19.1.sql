
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 48 ,    -- LootTableID - int
         'lth_ruined' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         15 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 49 ,    -- LootTableID - int
         'lth_flawed' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         30 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )