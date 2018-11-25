
-- Adjust component types for green lightsabers and saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryComponentTypeID	= 30
WHERE ID IN (622,623,624,625,626, 627,628,629,630,631)