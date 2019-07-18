
UPDATE dbo.CraftBlueprint
SET ItemName = REPLACE(ItemName, '+1', '+3')
WHERE ID IN (190,179,184,188,207,203,206,204,201,202)
