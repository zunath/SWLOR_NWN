
-- Reduce price on optimization and efficiency perks to 7 SP
UPDATE dbo.PerkLevel
SET Price = 7
WHERE PerkID IN (
137,138,139,140,141,143,144,145,146,147) 

-- Reduce level 1 of optimization and efficiency perks to 10
UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 10
WHERE PerkLevelID IN (
	SELECT pl.ID
	FROM dbo.PerkLevel pl
	WHERE pl.Level = 1
		AND pl.PerkID IN ( 137,138,139,140,141,143,144,145,146,147) 
)

-- Reduce level 2 of optimization and efficiency perks to 20
UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 20
WHERE PerkLevelID IN (
	SELECT pl.ID
	FROM dbo.PerkLevel pl
	WHERE pl.Level = 2
		AND pl.PerkID IN ( 137,138,139,140,141,143,144,145,146,147) 
)

-- Reduce construction parts base level and reduce required amount of metal
UPDATE dbo.CraftBlueprint
SET BaseLevel = 0,
	MainMinimum = 2
WHERE ID = 319

-- Reduce all blades to level 1 base
UPDATE dbo.CraftBlueprint
SET MainMinimum = 1
WHERE ID IN (92,93)

-- Reduce power core base level to 0
UPDATE dbo.CraftBlueprint
SET BaseLevel = 0
WHERE ID = 321

-- Add Mynock Tooth to Mynock CZ-220 loot table.
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 12 ,    -- LootTableID - int
         'mynock_tooth' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         12 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )