
UPDATE dbo.LootTables
SET Name = 'CZ-220 Scavenge - Scrap Metal'
WHERE LootTableID = 11

DELETE FROM dbo.LootTableItems
WHERE LootTableID = 11
	AND Resref = 'fiberp_destroyed'

INSERT INTO dbo.LootTables ( LootTableID ,
                             Name )
VALUES ( 45 , -- LootTableID - int
         N'CZ-220 Scavenge - Fiberplast' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItems ( LootTableID ,
                                 Resref ,
                                 MaxQuantity ,
                                 Weight ,
                                 IsActive ,
                                 SpawnRule )
VALUES ( 45 ,    -- LootTableID - int
         'fiberp_destroyed' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )