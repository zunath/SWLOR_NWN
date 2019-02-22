-- Disable the +2 and +3 BAB Mods
UPDATE dbo.CraftBlueprint
SET IsActive = 0
WHERE ID IN (175, 205)
