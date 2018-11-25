
-- Adjust component types for green lightsabers and saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryComponentTypeID	= 30
WHERE ID IN (622,623,624,625,626, 627,628,629,630,631)


-- Fix base levels on red lightsabers
UPDATE dbo.CraftBlueprint
SET BaseLevel = 2
WHERE ID = 612

UPDATE dbo.CraftBlueprint
SET BaseLevel = 7
WHERE ID = 613