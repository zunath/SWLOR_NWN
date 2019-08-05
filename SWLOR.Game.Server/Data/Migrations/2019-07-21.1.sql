
-- Increase interval and FP cost of Battle Insight and Force Insight
UPDATE dbo.PerkFeat
SET ConcentrationTickInterval = 6,
	ConcentrationFPCost = ConcentrationFPCost + 2
WHERE PerkID IN (86, 179)



-- Increase belt base levels.

-- Force Belt 3
UPDATE dbo.CraftBlueprint
SET BaseLevel = 23
WHERE ID = 271

-- Heavy Belt 2
UPDATE dbo.CraftBlueprint
SET BaseLevel = 16
WHERE ID = 273

-- Heavy Belt 3
UPDATE dbo.CraftBlueprint
SET BaseLevel = 24
WHERE ID = 274