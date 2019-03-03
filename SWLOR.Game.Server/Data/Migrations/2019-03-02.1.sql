

-- Refund Stronidium Refining
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 14 -- int
GO

-- Refund Speedy Refining
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 79 -- int
GO

-- Refund Refinery Management
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 81 -- int
GO

-- Refund Refining
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 82 -- int
GO

-- Change skill requirements from engineering to harvesting
UPDATE dbo.PerkLevelSkillRequirement
SET SkillID = 10
WHERE ID IN (
	SELECT plsr.ID
	FROM dbo.PerkLevelSkillRequirement plsr
	JOIN dbo.PerkLevel pl ON pl.ID = plsr.PerkLevelID
	WHERE pl.PerkID IN (14, 79, 81, 82)
)



-- Switch category to Harvesting
UPDATE dbo.Perk
SET PerkCategoryID = 34
WHERE ID IN (14, 79, 81, 82)


-- Fix the script names
UPDATE dbo.Perk
SET ScriptName = REPLACE(ScriptName, 'Engineering.', 'Harvesting.')
WHERE ID IN (14, 79, 81, 82)
