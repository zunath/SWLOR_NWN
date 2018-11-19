-- Move Electric Fist category from Gathering to Martial Arts
UPDATE dbo.Perk
SET PerkCategoryID = 17
WHERE ID = 93
