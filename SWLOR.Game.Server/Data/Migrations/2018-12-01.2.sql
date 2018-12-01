--Reduce casting time for Dash
UPDATE dbo.Perk
SET BaseCastingTime = 0
WHERE ID = 89


-- Remove the broken Mysterious Obelisk placeable (no longer available)
DELETE FROM dbo.PCBaseStructure
WHERE BaseStructureID = 152

DELETE FROM dbo.CraftBlueprint
WHERE ID = 470

DELETE FROM dbo.BaseStructure
WHERE ID = 152
