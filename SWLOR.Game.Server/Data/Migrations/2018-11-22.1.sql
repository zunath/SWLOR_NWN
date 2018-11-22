
UPDATE dbo.Perk
SET PerkCategoryID = 8
WHERE ID = 94

UPDATE dbo.PerkLevel
SET Description = 'Grants the Precision Targeting perk.'
WHERE PerkID = 133


-- Remove gold reward for first rites quest.
UPDATE dbo.Quest
SET RewardGold = 0
WHERE ID = 30



-- Adjust component types for red lightsabers and saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryComponentTypeID = 29
WHERE ID IN (612,613,614,615,616,617,618,619,620,621)

-- Adjust component types for green lightsabers and saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryComponentTypeID	= 31
WHERE ID IN (622,623,624,625,626, 627,628,629,630,631)

-- Adjust component types for yellow lightsabers and saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryComponentTypeID	= 31
WHERE ID IN (632,633,634,635,636, 637,638,639,640,641)
