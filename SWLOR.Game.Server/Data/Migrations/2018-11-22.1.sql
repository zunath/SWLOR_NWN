
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