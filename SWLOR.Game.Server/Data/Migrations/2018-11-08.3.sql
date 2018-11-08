
-- Delete a broken furniture placeable
DELETE FROM dbo.CraftBlueprint
where ID = 331

DELETE FROM dbo.BaseStructure
WHERE ID = 13

-- Remove all perk requirements for basic blueprints

UPDATE dbo.CraftBlueprint
SET PerkID = NULL,
	RequiredPerkLevel = 0
WHERE ID IN (
1,6,11,16,21,26,31,36,41,46,51,56,61,66,71,76,81,86,103,108,231,236,241,246,251,256,261,278,283,288,474,479,608,
319,321,324,325,326,330,335,336,339,340,341,342,343,344,345,347,348,349,350,365,369,373, 388,390,391,392,393,394,
395,396,397,398,399,400,401,402,403,404,499,522,527,532,533, 113,114,115,116,222,474,479,484,608)

-- Move tiers 1-4 items down two perk levels
UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = RequiredPerkLevel - 2
WHERE PerkID in (2, 9, 84, 96, 124)
	AND RequiredPerkLevel > 1

-- Reduce maximum perk level
DECLARE @PerkLevelIDs TABLE (
	ID INT
)

INSERT INTO @PerkLevelIDs ( ID )
SELECT pl.ID
FROM dbo.PerkLevel pl
WHERE pl.PerkID IN (2, 9, 84, 96, 124, 102)
	AND level IN (9, 10)

DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID 
	FROM @PerkLevelIDs 
)

DELETE FROM dbo.PerkLevel
WHERE id IN(
	SELECT ID 
	FROM @PerkLevelIDs 
)

-- Update description for perk levels
UPDATE dbo.Perk
SET Description = 'Unlocks new fabrication blueprints on every odd level (1, 3, 5, 7) and adds an enhancement slot for every even level (2, 4, 6, 8) for fabrication.'
WHERE id = 2

UPDATE dbo.Perk
SET Description = 'Unlocks new medicine blueprints on every odd level (1, 3, 5, 7) and adds an enhancement slot for every even level (2, 4, 6, 8) for medicine.'
WHERE ID = 9

UPDATE dbo.Perk
SET Description = 'Unlocks new weapon blueprints on every odd level (1, 3, 5, 7) and adds an enhancement slot for every even level (2, 4, 6, 8) for weaponsmithing.'
WHERE ID = 84

UPDATE dbo.Perk
SET Description = 'Unlocks new engineering blueprints on every odd level (1, 3, 5, 7) and adds an enhancement slot for every even level (2, 4, 6, 8) for engineering.'
WHERE ID = 96

UPDATE dbo.Perk
SET Description = 'Unlocks additional food recipes on every odd level (1, 3, 5, 7) and adds an enhancement slot for every even level (2, 4, 6, 8) for cooking.'
WHERE ID = 102

UPDATE dbo.Perk
SET Description = 'Unlocks new armor blueprints on every odd level (1, 3, 5, 7) and adds an enhancement slot for every even level (2, 4, 6, 8) for armorsmithing.'
WHERE ID = 124
