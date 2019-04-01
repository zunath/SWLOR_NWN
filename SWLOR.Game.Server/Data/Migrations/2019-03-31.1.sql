-- REBALANCING CHANGES


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



-- Update craft blueprint names for mods
UPDATE dbo.CraftBlueprint SET ItemName = 'Activation Speed +1' WHERE ID=121
UPDATE dbo.CraftBlueprint SET ItemName = 'Activation Speed +2' WHERE ID=152
UPDATE dbo.CraftBlueprint SET ItemName = 'Activation Speed +3' WHERE ID=184
UPDATE dbo.CraftBlueprint SET ItemName = 'Armor Class +1' WHERE ID=127
UPDATE dbo.CraftBlueprint SET ItemName = 'Armor Class +2' WHERE ID=160
UPDATE dbo.CraftBlueprint SET ItemName = 'Armor Class +3' WHERE ID=190
UPDATE dbo.CraftBlueprint SET ItemName = 'Armorsmith +3' WHERE ID=134
UPDATE dbo.CraftBlueprint SET ItemName = 'Armorsmith +6' WHERE ID=167
UPDATE dbo.CraftBlueprint SET ItemName = 'Armorsmith +9' WHERE ID=197
UPDATE dbo.CraftBlueprint SET ItemName = 'Attack Bonus +1' WHERE ID=117
UPDATE dbo.CraftBlueprint SET ItemName = 'Attack Bonus +2' WHERE ID=148
UPDATE dbo.CraftBlueprint SET ItemName = 'Attack Bonus +3' WHERE ID=179
UPDATE dbo.CraftBlueprint SET ItemName = 'Base Attack Bonus +1' WHERE ID=142
UPDATE dbo.CraftBlueprint SET ItemName = 'Base Attack Bonus +2' WHERE ID=175
UPDATE dbo.CraftBlueprint SET ItemName = 'Base Attack Bonus +3' WHERE ID=205
UPDATE dbo.CraftBlueprint SET ItemName = 'Charisma +3' WHERE ID=122
UPDATE dbo.CraftBlueprint SET ItemName = 'Charisma +6' WHERE ID=153
UPDATE dbo.CraftBlueprint SET ItemName = 'Charisma +9' WHERE ID=185
UPDATE dbo.CraftBlueprint SET ItemName = 'Constitution +3' WHERE ID=118
UPDATE dbo.CraftBlueprint SET ItemName = 'Constitution +6' WHERE ID=149
UPDATE dbo.CraftBlueprint SET ItemName = 'Constitution +9' WHERE ID=181
UPDATE dbo.CraftBlueprint SET ItemName = 'Cooking +3' WHERE ID=131
UPDATE dbo.CraftBlueprint SET ItemName = 'Cooking +6' WHERE ID=164
UPDATE dbo.CraftBlueprint SET ItemName = 'Cooking +9' WHERE ID=194
UPDATE dbo.CraftBlueprint SET ItemName = 'Damage +1' WHERE ID=125
UPDATE dbo.CraftBlueprint SET ItemName = 'Damage +2' WHERE ID=158
UPDATE dbo.CraftBlueprint SET ItemName = 'Damage +3' WHERE ID=188
UPDATE dbo.CraftBlueprint SET ItemName = 'Dark Defense +3' WHERE ID=333
UPDATE dbo.CraftBlueprint SET ItemName = 'Dark Defense +6' WHERE ID=334
UPDATE dbo.CraftBlueprint SET ItemName = 'Dark Defense +9' WHERE ID=352
UPDATE dbo.CraftBlueprint SET ItemName = 'Dark Potency +3' WHERE ID=129
UPDATE dbo.CraftBlueprint SET ItemName = 'Dark Potency +6' WHERE ID=162
UPDATE dbo.CraftBlueprint SET ItemName = 'Dark Potency +9' WHERE ID=192
UPDATE dbo.CraftBlueprint SET ItemName = 'Dexterity +3' WHERE ID=119
UPDATE dbo.CraftBlueprint SET ItemName = 'Dexterity +6' WHERE ID=150
UPDATE dbo.CraftBlueprint SET ItemName = 'Dexterity +9' WHERE ID=182
UPDATE dbo.CraftBlueprint SET ItemName = 'Durability +1' WHERE ID=144
UPDATE dbo.CraftBlueprint SET ItemName = 'Durability +2' WHERE ID=177
UPDATE dbo.CraftBlueprint SET ItemName = 'Durability +3' WHERE ID=207
UPDATE dbo.CraftBlueprint SET ItemName = 'Electrical Defense +3' WHERE ID=389
UPDATE dbo.CraftBlueprint SET ItemName = 'Electrical Defense +6' WHERE ID=405
UPDATE dbo.CraftBlueprint SET ItemName = 'Electrical Defense +9' WHERE ID=412
UPDATE dbo.CraftBlueprint SET ItemName = 'Electrical Potency +3' WHERE ID=195
UPDATE dbo.CraftBlueprint SET ItemName = 'Electrical Potency +6' WHERE ID=323
UPDATE dbo.CraftBlueprint SET ItemName = 'Electrical Potency +9' WHERE ID=329
UPDATE dbo.CraftBlueprint SET ItemName = 'Engineering +3' WHERE ID=136
UPDATE dbo.CraftBlueprint SET ItemName = 'Engineering +6' WHERE ID=169
UPDATE dbo.CraftBlueprint SET ItemName = 'Engineering +9' WHERE ID=199
UPDATE dbo.CraftBlueprint SET ItemName = 'Enhancement Bonus +1' WHERE ID=140
UPDATE dbo.CraftBlueprint SET ItemName = 'Enhancement Bonus +2' WHERE ID=173
UPDATE dbo.CraftBlueprint SET ItemName = 'Enhancement Bonus +3' WHERE ID=203
UPDATE dbo.CraftBlueprint SET ItemName = 'Fabrication +3' WHERE ID=308
UPDATE dbo.CraftBlueprint SET ItemName = 'Fabrication +6' WHERE ID=309
UPDATE dbo.CraftBlueprint SET ItemName = 'Fabrication +9' WHERE ID=310
UPDATE dbo.CraftBlueprint SET ItemName = 'Medicine +3' WHERE ID=137
UPDATE dbo.CraftBlueprint SET ItemName = 'Medicine +6' WHERE ID=170
UPDATE dbo.CraftBlueprint SET ItemName = 'Medicine +9' WHERE ID=200
UPDATE dbo.CraftBlueprint SET ItemName = 'FP +1' WHERE ID=130
UPDATE dbo.CraftBlueprint SET ItemName = 'FP +2' WHERE ID=163
UPDATE dbo.CraftBlueprint SET ItemName = 'FP +3' WHERE ID=193
UPDATE dbo.CraftBlueprint SET ItemName = 'FP Regen +1' WHERE ID=143
UPDATE dbo.CraftBlueprint SET ItemName = 'FP Regen +2' WHERE ID=176
UPDATE dbo.CraftBlueprint SET ItemName = 'FP Regen +3' WHERE ID=206
UPDATE dbo.CraftBlueprint SET ItemName = 'Harvesting +3' WHERE ID=133
UPDATE dbo.CraftBlueprint SET ItemName = 'Harvesting +6' WHERE ID=166
UPDATE dbo.CraftBlueprint SET ItemName = 'Harvesting +9' WHERE ID=196
UPDATE dbo.CraftBlueprint SET ItemName = 'Hit Points +1' WHERE ID=126
UPDATE dbo.CraftBlueprint SET ItemName = 'Hit Points +2' WHERE ID=159
UPDATE dbo.CraftBlueprint SET ItemName = 'Hit Points +3' WHERE ID=189
UPDATE dbo.CraftBlueprint SET ItemName = 'HP Regen +1' WHERE ID=141
UPDATE dbo.CraftBlueprint SET ItemName = 'HP Regen +2' WHERE ID=174
UPDATE dbo.CraftBlueprint SET ItemName = 'HP Regen +3' WHERE ID=204
UPDATE dbo.CraftBlueprint SET ItemName = 'Improved Enmity +1' WHERE ID=138
UPDATE dbo.CraftBlueprint SET ItemName = 'Improved Enmity +2' WHERE ID=171
UPDATE dbo.CraftBlueprint SET ItemName = 'Improved Enmity +3' WHERE ID=201
UPDATE dbo.CraftBlueprint SET ItemName = 'Intelligence +3' WHERE ID=123
UPDATE dbo.CraftBlueprint SET ItemName = 'Intelligence +6' WHERE ID=154
UPDATE dbo.CraftBlueprint SET ItemName = 'Intelligence +9' WHERE ID=186
UPDATE dbo.CraftBlueprint SET ItemName = 'Level Decrease -5' WHERE ID=147
UPDATE dbo.CraftBlueprint SET ItemName = 'Level Increase +5' WHERE ID=146
UPDATE dbo.CraftBlueprint SET ItemName = 'Light Defense +3' WHERE ID=364
UPDATE dbo.CraftBlueprint SET ItemName = 'Light Defense +6' WHERE ID=367
UPDATE dbo.CraftBlueprint SET ItemName = 'Light Defense +9' WHERE ID=368
UPDATE dbo.CraftBlueprint SET ItemName = 'Light Potency +3' WHERE ID=128
UPDATE dbo.CraftBlueprint SET ItemName = 'Light Potency +6' WHERE ID=161
UPDATE dbo.CraftBlueprint SET ItemName = 'Light Potency +9' WHERE ID=191
UPDATE dbo.CraftBlueprint SET ItemName = 'Luck +1' WHERE ID=156
UPDATE dbo.CraftBlueprint SET ItemName = 'Luck +2' WHERE ID=209
UPDATE dbo.CraftBlueprint SET ItemName = 'Meditate +1' WHERE ID=157
UPDATE dbo.CraftBlueprint SET ItemName = 'Meditate +2' WHERE ID=210
UPDATE dbo.CraftBlueprint SET ItemName = 'Mind Defense +3' WHERE ID=371
UPDATE dbo.CraftBlueprint SET ItemName = 'Mind Defense +6' WHERE ID=381
UPDATE dbo.CraftBlueprint SET ItemName = 'Mind Defense +9' WHERE ID=384
UPDATE dbo.CraftBlueprint SET ItemName = 'Mind Potency +3' WHERE ID=132
UPDATE dbo.CraftBlueprint SET ItemName = 'Mind Potency +6' WHERE ID=165
UPDATE dbo.CraftBlueprint SET ItemName = 'Mind Potency +9' WHERE ID=180
UPDATE dbo.CraftBlueprint SET ItemName = 'Reduced Enmity -1' WHERE ID=145
UPDATE dbo.CraftBlueprint SET ItemName = 'Reduced Enmity -2' WHERE ID=178
UPDATE dbo.CraftBlueprint SET ItemName = 'Reduced Enmity -3' WHERE ID=208
UPDATE dbo.CraftBlueprint SET ItemName = 'Sneak Attack +1' WHERE ID=139
UPDATE dbo.CraftBlueprint SET ItemName = 'Sneak Attack +2' WHERE ID=172
UPDATE dbo.CraftBlueprint SET ItemName = 'Sneak Attack +3' WHERE ID=202
UPDATE dbo.CraftBlueprint SET ItemName = 'Strength +3' WHERE ID=120
UPDATE dbo.CraftBlueprint SET ItemName = 'Strength +6' WHERE ID=151
UPDATE dbo.CraftBlueprint SET ItemName = 'Strength +9' WHERE ID=183
UPDATE dbo.CraftBlueprint SET ItemName = 'Weaponsmith +3' WHERE ID=135
UPDATE dbo.CraftBlueprint SET ItemName = 'Weaponsmith +6' WHERE ID=168
UPDATE dbo.CraftBlueprint SET ItemName = 'Weaponsmith +9' WHERE ID=198
UPDATE dbo.CraftBlueprint SET ItemName = 'Wisdom +3' WHERE ID=124
UPDATE dbo.CraftBlueprint SET ItemName = 'Wisdom +6' WHERE ID=155
UPDATE dbo.CraftBlueprint SET ItemName = 'Wisdom +9' WHERE ID=187