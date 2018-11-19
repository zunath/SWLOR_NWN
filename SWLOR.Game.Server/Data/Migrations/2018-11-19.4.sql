
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



UPDATE dbo.GameTopic
SET Text = 'Enhancements are OPTIONAL items you may use for a blueprint. These are usually obtained from completing quests or looting defeated enemies. Enhancements can be added as part of the crafting process as long as two requirements are met:\n\n1.) The blueprint allows for enhancement slots.\n2.) You have the necessary skill rank and perk level to use the enhancement slot.\n\nIf both requirements are met, an option will appear on the crafting menu allowing you to do so.'
WHERE ID = 46