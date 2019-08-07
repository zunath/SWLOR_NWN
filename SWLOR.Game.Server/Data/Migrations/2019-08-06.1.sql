
-- Add missing loot table for kath hound meat quest.
INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 44 , -- ID - int
         N'Kath Hounds 2 - Viscara' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 44 ,    -- LootTableID - int
         'kath_meat_1' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )