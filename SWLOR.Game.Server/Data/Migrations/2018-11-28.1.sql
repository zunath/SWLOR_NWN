
-- Reduce maximum reinforced mode for towers from 2 days to 12 hours
UPDATE dbo.BaseStructure
SET ReinforcedStorage = 24
WHERE ID IN (1, 2, 3)

-- Update existing towers to have 24 units of fuel if they're over the new cap
UPDATE dbo.PCBase
SET ReinforcedFuel = 24
WHERE ReinforcedFuel > 24