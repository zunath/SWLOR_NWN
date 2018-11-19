
UPDATE dbo.Perk
SET Description = 'Your next attack will tranquilize all creatures within range of your target, putting them to sleep for a short time. Damage will break the effect prematurely. Must be equipped with a Blaster Rifle to use. Length of effect depends on Tranquilizer perk.',
	ScriptName = 'Blaster.MassTranquilizer'
WHERE ID = 75