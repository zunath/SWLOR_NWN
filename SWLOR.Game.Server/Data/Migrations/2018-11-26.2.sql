
UPDATE dbo.PerkLevelSkillRequirement
SET SkillID = 14
WHERE PerkLevelID IN (
	SELECT pl.ID
	FROM dbo.PerkLevel pl
	WHERE PerkID IN (43, 117, 63)
)