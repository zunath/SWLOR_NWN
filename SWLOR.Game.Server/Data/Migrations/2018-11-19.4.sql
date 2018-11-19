
UPDATE dbo.Perk
SET Description = 'Your next attack will tranquilize all creatures within range of your target, putting them to sleep for a short time. Damage will break the effect prematurely. Must be equipped with a Blaster Rifle to use. Length of effect depends on Tranquilizer perk.',
	ScriptName = 'Blaster.MassTranquilizer'
WHERE ID = 75



UPDATE dbo.Perk
SET Description = 'Your next attack will tranquilize all creatures within range of your target, putting them to sleep for a short time. Damage will break the effect prematurely. Must be equipped with a Blaster Rifle to use. Length of effect depends on Tranquilizer perk.',
	ScriptName = 'Blaster.MassTranquilizer'
WHERE ID = 75


UPDATE dbo.PerkLevel
SET Level = 2 WHERE PerkID = 123 AND Level = 3

UPDATE dbo.PerkLevel
SET Level = 3 WHERE PerkID = 123 AND Level = 4

UPDATE dbo.PerkLevel
SET Level = 4, Description = 'Deals 3x damage from behind, 2x damage from any other direction. Reduces cooldown by 1 minute.'
WHERE PerkID = 123 AND Level = 5

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 40
WHERE PerkLevelID IN (
	SELECT ID 
	FROM dbo.PerkLevel
	WHERE PerkID = 123
		AND Level = 4 
)
