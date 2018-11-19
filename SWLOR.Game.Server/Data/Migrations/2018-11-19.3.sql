
-- Fix Dash perk description
UPDATE dbo.Perk
SET Description = 'Increases your movement speed for a short period of time.'
WHERE ID = 89


-- Fix required rank on Speedy First Aid perk.
UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = RequiredRank / 2
WHERE PerkLevelID IN (
	SELECT ID 
	FROM dbo.PerkLevel
	WHERE PerkID = 20
)

-- Fix description on rank 6 of Speedy First Aid perk.
UPDATE dbo.PerkLevel
SET Description = '+60% Chance'
WHERE PerkID = 20
	AND Level = 6
