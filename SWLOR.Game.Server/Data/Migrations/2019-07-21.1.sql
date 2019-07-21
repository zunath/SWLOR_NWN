
-- Increase interval and FP cost of Battle Insight and Force Insight
UPDATE dbo.PerkFeat
SET ConcentrationTickInterval = 6,
	ConcentrationFPCost = ConcentrationFPCost + 2
WHERE PerkID IN (86, 179)
