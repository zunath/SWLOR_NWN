
-- Consolidate perk categories for force.
UPDATE dbo.PerkCategory
SET Name = 'Force Alter'
WHERE ID = 40

UPDATE dbo.PerkCategory
SET Name = 'Force Control'
WHERE ID = 43

UPDATE dbo.PerkCategory
SET Name = 'Force Sense'
WHERE ID = 46

UPDATE dbo.Perk
SET PerkCategoryID = 40
WHERE PerkCategoryID IN (41, 42, 49)

UPDATE dbo.Perk
SET PerkCategoryID = 43
WHERE PerkCategoryID IN (44, 45, 50)

UPDATE dbo.Perk
SET PerkCategoryID = 46
WHERE PerkCategoryID IN (47, 48, 51)

DELETE FROM dbo.PerkCategory
WHERE ID IN (41, 42, 49, 44, 45, 50, 47, 48, 51)


-- Fix Rage's interval to 6 seconds.
UPDATE dbo.PerkFeat
SET ConcentrationTickInterval = 6
WHERE PerkID = 19


-- Fix description of Force Speed.
UPDATE dbo.PerkLevel
SET Description = 'Increases movement speed by 50%, Dexterity by 10 and grants an extra attack.'
WHERE PerkID = 3
	AND Level = 5


-- Reduce tick interval for force lightning to 3 per spreadsheet changes.
UPDATE dbo.PerkFeat
SET ConcentrationTickInterval = 3
WHERE PerkID = 182
