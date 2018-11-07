-- Add enmity to the force push perk
UPDATE dbo.Perk
SET EnmityAdjustmentRuleID = 1,
	Enmity = 15
WHERE ID = 19