
-- Increase reinforced storage to 7200 units of Stronidium since they burn at 1 unit per 6 seconds now.
UPDATE dbo.BaseStructure
SET ReinforcedStorage = 7200
WHERE ID IN (1, 2, 3)

-- Convert existing Stronidium storage to the new rate. I.E: 24 units -> 7200 units after the change.
UPDATE dbo.PCBase
SET ReinforcedFuel = ReinforcedFuel * 300
WHERE Sector IN ('SE', 'SW', 'NE', 'NW')