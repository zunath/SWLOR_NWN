
UPDATE dbo.PerkLevelSkillRequirement
SET SkillID = 14
WHERE ID IN (
	SELECT ps.ID
	FROM dbo.PerkLevelSkillRequirement ps
	JOIN dbo.PerkLevel pl ON pl.ID = ps.PerkLevelID
	WHERE pl.PerkID = 151
	
)


UPDATE dbo.CraftBlueprint
SET SkillID = 14
WHERE PerkID = 151



UPDATE dbo.CraftBlueprint
SET ItemResref = 'rune_manareg1'
WHERE ID = 143

UPDATE dbo.CraftBlueprint
SET ItemResref = 'rune_manareg2'
WHERE ID = 176

UPDATE dbo.CraftBlueprint
SET ItemResref = 'rune_manareg3'
WHERE ID = 206