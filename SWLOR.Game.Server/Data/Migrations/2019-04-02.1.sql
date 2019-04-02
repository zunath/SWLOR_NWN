
-- Disable the force Defense/Potency mod blueprints
UPDATE dbo.CraftBlueprint
SET IsActive = 0
WHERE ID IN (333,334,352,129,162,192,389,405,412,195,323,329,364,367,368,128,161,191,371,381,384,132,165,180)

-- Rename Activation Speed to Cooldown Reduction
UPDATE dbo.CraftBlueprint
SET ItemName = 'Cooldown Reduction +1'
WHERE ID = 121

UPDATE dbo.CraftBlueprint
SET ItemName = 'Cooldown Reduction +2'
WHERE ID = 152

UPDATE dbo.CraftBlueprint
SET ItemName = 'Cooldown Reduction +3'
WHERE ID = 184
