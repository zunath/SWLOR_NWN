
UPDATE dbo.CraftBlueprint
SET SkillID = 22
WHERE ID IN (
103,104,105,106,107,108,109,110,111,112,211,212,213,214,215,216,217,218,219,220,221,538,539,
540,541,542,543,544,545,558,559,560,561,570,571,572,573)

UPDATE dbo.CraftBlueprint
SET PerkID = 96
WHERE ID IN (
104,105,106,107,109,110,111,112,538,539,540,541,542,543,544,545)

UPDATE dbo.Perk
SET ScriptName = 'Engineering.LightsaberBlueprints',
	Description = 'Unlocks new lightsaber blueprints on every odd level (1, 3, 5, 7, 9) and adds an enhancement slot for every even level (2, 4, 6, 8, 10) for engineering.'
WHERE ID = 151

UPDATE dbo.PerkLevelSkillRequirement
SET SkillID = 22
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = 151 
)
