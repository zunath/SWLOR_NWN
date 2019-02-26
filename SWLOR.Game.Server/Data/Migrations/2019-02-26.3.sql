-- Disable the +2 and +3 BAB Mods
UPDATE dbo.CraftBlueprint
SET IsActive = 0
WHERE ID IN (175, 205)



-- Refund Health perk for all players.
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 16 -- int

-- Refund FP perk for all players.
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 17 -- int

-- Change price of Health and FP perks to 3 SP
UPDATE dbo.PerkLevel
SET Price = 3
WHERE ID IN (16, 17)



-- Update perk level descriptions to reflect changes to recovery amounts.
UPDATE dbo.PerkLevel
SET Description = 'Restores 6 HP every 6 seconds. Recast time: 5 minutes'
WHERE PerkID = 7 AND Level = 1
UPDATE dbo.PerkLevel
SET Description = 'Restores 6 HP every 6 seconds. Recast time: 4 minutes, 30 seconds'
WHERE PerkID = 7 AND Level = 2
UPDATE dbo.PerkLevel
SET Description = 'Restores 6 HP every 6 seconds. Recast time: 4 minutes'
WHERE PerkID = 7 AND Level = 3
UPDATE dbo.PerkLevel
SET Description = 'Restores 10 HP every 6 seconds. Recast time: 4 minutes'
WHERE PerkID = 7 AND Level = 4
UPDATE dbo.PerkLevel
SET Description = 'Restores 10 HP every 6 seconds. Recast time: 3 minutes, 30 seconds'
WHERE PerkID = 7 AND Level = 5
UPDATE dbo.PerkLevel
SET Description = 'Restores 10 HP every 6 seconds. Recast time: 3 minutes'
WHERE PerkID = 7 AND Level = 6
UPDATE dbo.PerkLevel
SET Description = 'Restores 14 HP every 6 seconds. Recast time: 3 minutes'
WHERE PerkID = 7 AND Level = 7

UPDATE dbo.PerkLevel
SET Description = 'Restores 6 FP every 6 seconds. Recast time: 5 minutes'
WHERE PerkID = 103 AND Level = 1
UPDATE dbo.PerkLevel
SET Description = 'Restores 6 FP every 6 seconds. Recast time: 4 minutes, 30 seconds'
WHERE PerkID = 103 AND Level = 2
UPDATE dbo.PerkLevel
SET Description = 'Restores 6 FP every 6 seconds. Recast time: 4 minutes'
WHERE PerkID = 103 AND Level = 3
UPDATE dbo.PerkLevel
SET Description = 'Restores 10 FP every 6 seconds. Recast time: 4 minutes'
WHERE PerkID = 103 AND Level = 4
UPDATE dbo.PerkLevel
SET Description = 'Restores 10 FP every 6 seconds. Recast time: 3 minutes, 30 seconds'
WHERE PerkID = 103 AND Level = 5
UPDATE dbo.PerkLevel
SET Description = 'Restores 10 FP every 6 seconds. Recast time: 3 minutes'
WHERE PerkID = 103 AND Level = 6
UPDATE dbo.PerkLevel
SET Description = 'Restores 14 FP every 6 seconds. Recast time: 3 minutes'
WHERE PerkID = 103 AND Level = 7
