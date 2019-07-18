
-- Fix crafted mod item names
UPDATE dbo.CraftBlueprint
SET ItemName = REPLACE(ItemName, '+1', '+3')
WHERE ID IN (190,179,184,188,207,203,206,204,201,202)



-- Fix enhancement scaling for armor blueprints
UPDATE dbo.CraftBlueprint SET EnhancementSlots = 1 
WHERE ID IN (231,278,643,236,288,649,251,241,283,654,261,246,256)

UPDATE dbo.CraftBlueprint SET EnhancementSlots = 2
WHERE ID IN (232, 237, 252, 242, 262, 247, 257)

UPDATE dbo.CraftBlueprint SET EnhancementSlots = 3
WHERE ID IN (233, 238, 253, 243, 263, 248, 258)

UPDATE dbo.CraftBlueprint SET EnhancementSlots = 4
WHERE ID IN (234, 239, 254, 244, 264, 249, 259)

UPDATE dbo.CraftBlueprint SET EnhancementSlots = 5
WHERE ID IN (235, 240, 255, 245, 265, 250, 260)