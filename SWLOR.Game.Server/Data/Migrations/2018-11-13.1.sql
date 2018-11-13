
-- Increase structure count for small and medium buildings
UPDATE dbo.BaseStructure
SET Storage = 40 WHERE ID = 153

UPDATE dbo.BaseStructure
SET Storage = 50 WHERE ID = 154


-- Fix the device from weaponsmith to engineering for lightsaber and firearms
UPDATE dbo.CraftBlueprint
SET CraftDeviceID = 4
WHERE SkillID = 22
	AND CraftDeviceID = 2
