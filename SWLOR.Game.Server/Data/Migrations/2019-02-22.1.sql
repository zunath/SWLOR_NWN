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
