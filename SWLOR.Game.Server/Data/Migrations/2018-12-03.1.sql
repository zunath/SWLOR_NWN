
-- Add lease rate field to Player table
ALTER TABLE dbo.Player
ADD LeaseRate INT NOT NULL DEFAULT 0

-- Adjust components for ranged weapon cores
UPDATE dbo.CraftBlueprint
SET MainMinimum = 2, 
	MainMaximum = 4, 
	SecondaryComponentTypeID = 15,
	SecondaryMinimum = 1,
	SecondaryMaximum = 2
WHERE ID = 100


-- Add new loot to existing tables
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 48 ,    -- LootTableID - int
         'warocas_meat' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         25 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )



UPDATE dbo.LootTableItem
SET Weight = 50
WHERE LootTableID = 23
	AND Resref = 'elec_flawed'

UPDATE dbo.LootTableItem
SET Weight = 40
WHERE LootTableID = 23
	AND Resref = 'elec_poor'
