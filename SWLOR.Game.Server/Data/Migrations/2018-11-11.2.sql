
-- Cut Tier 1+ item base levels in half for weaponsmith
UPDATE dbo.CraftBlueprint
SET BaseLevel = BaseLevel / 2
WHERE SkillID = 12
	AND ItemName NOT LIKE '%repair kit%'
	AND CraftCategoryID NOT IN (13)
	AND ItemName NOT LIKE '%basic%'

-- Cut Tier 1+ item base levels in half for armorsmith
UPDATE dbo.CraftBlueprint
SET BaseLevel = BaseLevel / 2
WHERE SkillID = 13
	AND ItemName NOT LIKE '%repair kit%'
	AND CraftCategoryID NOT IN (13)
	AND ItemName NOT LIKE '%basic%'