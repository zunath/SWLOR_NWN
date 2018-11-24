
-- Switch gargoyle to the correct placeable so it doesn't spawn an enemy (lol)
UPDATE dbo.BaseStructure
SET PlaceableResref = 'plc_statue3'
WHERE ID = 14