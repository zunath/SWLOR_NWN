
-- Emitter: Drop to level 1
UPDATE dbo.CraftBlueprint
SET BaseLevel = 1
WHERE ID = 611

-- Power clusters: Drop to level 1
UPDATE dbo.CraftBlueprint
SET BaseLevel = 1
WHERE ID IN (
113,114,115,116,222)

-- Saber hilt: Drop to level 1
UPDATE dbo.CraftBlueprint
SET BaseLevel = 1
WHERE ID = 221

-- Lightsabers: Drop to level 2
UPDATE dbo.CraftBlueprint
SET BaseLevel = 2
WHERE ID IN( 211, 613, 622, 632)
