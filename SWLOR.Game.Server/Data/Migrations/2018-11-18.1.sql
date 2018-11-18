
-- Reduce base level of lightsaber and saberstaff repair kits by 3.
UPDATE dbo.CraftBlueprint
SET BaseLevel = BaseLevel - 3
WHERE ID IN (558,559,560,561,570,571,572,573)
