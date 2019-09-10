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
UPDATE dbo.CraftBlueprint SET ItemName = 'Cooldown Reduction +1' WHERE ID=121
UPDATE dbo.CraftBlueprint SET ItemName = 'Cooldown Reduction +2' WHERE ID=152
UPDATE dbo.CraftBlueprint SET ItemName = 'Cooldown Reduction +3' WHERE ID=184
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
UPDATE dbo.CraftBlueprint SET ItemName = 'FP +5' WHERE ID=130
UPDATE dbo.CraftBlueprint SET ItemName = 'FP +10' WHERE ID=163
UPDATE dbo.CraftBlueprint SET ItemName = 'FP +15' WHERE ID=193
UPDATE dbo.CraftBlueprint SET ItemName = 'FP Regen +1' WHERE ID=143
UPDATE dbo.CraftBlueprint SET ItemName = 'FP Regen +2' WHERE ID=176
UPDATE dbo.CraftBlueprint SET ItemName = 'FP Regen +3' WHERE ID=206
UPDATE dbo.CraftBlueprint SET ItemName = 'Harvesting +3' WHERE ID=133
UPDATE dbo.CraftBlueprint SET ItemName = 'Harvesting +6' WHERE ID=166
UPDATE dbo.CraftBlueprint SET ItemName = 'Harvesting +9' WHERE ID=196
UPDATE dbo.CraftBlueprint SET ItemName = 'Hit Points +5' WHERE ID=126
UPDATE dbo.CraftBlueprint SET ItemName = 'Hit Points +10' WHERE ID=159
UPDATE dbo.CraftBlueprint SET ItemName = 'Hit Points +15' WHERE ID=189
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

GO



-- Refund Force Breach
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 3 -- int

-- Refund Force Lightning
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 4 -- int

-- Refund Force Heal
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 5 -- int

-- Refund Absorption Field
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 11 -- int

-- Refund Force Spread
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 13 -- int

-- Refund Force Push
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 19 -- int

-- Refund Force Aura
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 76 -- int

-- Refund Drain Life
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 78 -- int

-- Refund Chainspell
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 126 -- int


UPDATE dbo.Skill
SET Name = 'Force Alter',
	Description = 'Ability to use alter-based force abilities like Force Confusion and Force Push. Higher skill levels unlock new abilities.'
WHERE ID = 19 -- Force Combat

UPDATE dbo.Skill
SET Name = 'Force Control',
	Description = 'Ability to use control-based force abilities like Force Speed and Rage. Higher skill levels unlock new abilities.'
WHERE ID = 20 -- Force Support

UPDATE dbo.Skill
SET Name = 'Force Sense',
	Description = 'Ability to use sense-based force abilities like Force Stun and Premonition. Higher skill levels unlock new abilities.'
WHERE ID = 21 -- Force Utility


-- Sum up levels in Force Combat / Force Support / Force Utility and put them into the Skill Pool for later redistribution by the player.
INSERT INTO dbo.PCSkillPool ( ID ,
                              PlayerID ,
                              SkillCategoryID ,
                              Levels )
SELECT 
	NEWID(),
	pcs.PlayerID, 
	6, -- 6 = Force
	SUM(pcs.Rank)
FROM dbo.PCSkill pcs
WHERE pcs.SkillID IN (19, 20, 21)
GROUP BY pcs.PlayerID 
HAVING SUM(pcs.Rank) > 0

-- Set the skill levels to zero since they've been pooled.
UPDATE dbo.PCSkill
SET Rank = 0,
	XP = 0
WHERE SkillID IN (19, 20, 21)


-- Some of our perk names are carried over, but all of the functionality has changed.
-- Just to make my life a little easier, we're going to wipe all existing perk information
-- and start from scratch.

DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (
		3, 4, 5, 11, 13, 19, 76, 78, 126
	) 
)

DELETE FROM dbo.PCPerkRefund
WHERE PerkID IN (3, 4, 5, 11, 13, 19, 76, 78, 126)

DELETE FROM dbo.PerkFeat
WHERE PerkID IN (3, 4, 5, 11, 13, 19, 76, 78, 126)

DELETE FROM dbo.PerkLevelQuestRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel 
	WHERE PerkID IN (
		3, 4, 5, 11, 13, 19, 76, 78, 126
	)
)

DELETE FROM dbo.PerkLevel
WHERE PerkID IN(3, 4, 5, 11, 13, 19, 76, 78, 126)

DELETE FROM dbo.Perk
WHERE ID IN (3, 4, 5, 11, 13, 19, 76, 78, 126)

-- Set up the Specialization table
CREATE TABLE Specialization(
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL
)

INSERT INTO dbo.Specialization ( ID ,
                                 Name )
VALUES ( 0 , -- ID - int
         N'None' -- Name - nvarchar(32)
    )
INSERT INTO dbo.Specialization ( ID ,
                                 Name )
VALUES ( 1 , -- ID - int
         N'Guardian' -- Name - nvarchar(32)
    )
INSERT INTO dbo.Specialization ( ID ,
                                 Name )
VALUES ( 2 , -- ID - int
         N'Consular' -- Name - nvarchar(32)
    )
INSERT INTO dbo.Specialization ( ID ,
                                 Name )
VALUES ( 3 , -- ID - int
         N'Sentinel' -- Name - nvarchar(32)
    )

CREATE TABLE ForceBalanceType(
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL
)

INSERT INTO dbo.ForceBalanceType ( ID ,
                                   Name )
VALUES ( 0 , -- ID - int
         N'Universal' -- Name - nvarchar(32)
    )

INSERT INTO dbo.ForceBalanceType ( ID ,
                                   Name )
VALUES ( 1 , -- ID - int
         N'Light Side' -- Name - nvarchar(32)
    )

INSERT INTO dbo.ForceBalanceType ( ID ,
                                   Name )
VALUES ( 2 , -- ID - int
         N'Dark Side' -- Name - nvarchar(32)
    )

ALTER TABLE dbo.Perk
ADD ForceBalanceTypeID INT NOT NULL DEFAULT 0
CONSTRAINT FK_Perk_ForceBalanceTypeID FOREIGN KEY REFERENCES dbo.ForceBalanceType(ID)

ALTER TABLE dbo.Player
ADD SpecializationID INT NOT NULL DEFAULT 0
CONSTRAINT FK_Player_SpecializationID FOREIGN KEY REFERENCES dbo.Specialization(ID)

-- Remove the old perk categories
DELETE FROM dbo.PerkCategory WHERE ID IN (29, 30, 31)

-- Move the sequences for other categories up.
UPDATE dbo.PerkCategory
SET Sequence = 28 WHERE ID = 36
UPDATE dbo.PerkCategory
SET Sequence = 29 WHERE ID = 37

-- Add the new perk categories.
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 40 ,    -- ID - int
         N'Force Alter - General' ,  -- Name - nvarchar(64)
         1, -- IsActive - bit
         30      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 41 ,    -- ID - int
         N'Force Alter - Guardian' ,  -- Name - nvarchar(64)
         1, -- IsActive - bit
         31      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 42 ,    -- ID - int
         N'Force Alter - Consular' ,  -- Name - nvarchar(64)
         1 , -- IsActive - bit
         32      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 43 ,    -- ID - int
         N'Force Control - General' ,  -- Name - nvarchar(64)
         1, -- IsActive - bit
         33      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 44 ,    -- ID - int
         N'Force Control - Guardian' ,  -- Name - nvarchar(64)
         1, -- IsActive - bit
         34      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 45 ,    -- ID - int
         N'Force Control - Consular' ,  -- Name - nvarchar(64)
         1 , -- IsActive - bit
         35      -- Sequence - int
    )
	

-- Disable the force Defense/Potency mod blueprints
UPDATE dbo.CraftBlueprint
SET IsActive = 0
WHERE ID IN (333,334,352,129,162,192,389,405,412,195,323,329,364,367,368,128,161,191,371,381,384,132,165,180)

-- Rename Activation Speed to Cooldown Reduction
UPDATE dbo.CraftBlueprint
SET ItemName = 'Cooldown Reduction +1'
WHERE ID = 121

UPDATE dbo.CraftBlueprint
SET ItemName = 'Cooldown Reduction +2'
WHERE ID = 152

UPDATE dbo.CraftBlueprint
SET ItemName = 'Cooldown Reduction +3'
WHERE ID = 184

GO  

-- Add perks for the force rebalance.
-- Force Alter 40 = General, 41 = Guardian, 42 = Consular 49 = Sentinel
-- Force Control 43 = General 44 = Guardian 45 = consular, 50 = sentinel
-- Force Sense 46 = General, 47 = Guardian 48 = Consular 51 = Sentinel
INSERT INTO dbo.PerkCategory ( ID ,  Name ,  IsActive ,  Sequence ) VALUES
(46, N'Force Sense - General', 1, 36),
(47, N'Force Sense - Guardian', 1, 37),
(48, N'Force Sense - Consular', 1, 38),
(49, N'Force Alter - Sentinel', 1, 39),
(50, N'Force Control - Sentinel', 1, 40),
(51, N'Force Sense - Sentinel', 1, 41);

-- Remove PC cooldowns
DELETE FROM dbo.PCCooldown
WHERE CooldownCategoryID IN (2 ,4 ,5 ,6 ,7 ,8 ,9 ,10,11,12,16,17,20,21,28,34,35,36)

-- Remove the old cooldown categories
DELETE FROM dbo.CooldownCategory
WHERE ID IN (2 ,4 ,5 ,6 ,7 ,8 ,9 ,10,11,12,16,17,20,21,28,34,35,36)


-- Add new cooldown categories
INSERT INTO dbo.CooldownCategory ( ID ,Name ,BaseCooldownTime ) VALUES
( 2 , N'Force Speed' , 600.0),
( 4 , N'Absorb Energy' , 600.0),
( 5 , N'Force Body' , 600.0),
( 6 , N'Mind Shield' , 600.0),
( 7 , N'Rage' , 600.0),
( 8 , N'Force Persuade' , 600.0),
( 9 , N'Confusion' , 600.0),
( 10 , N'Force Stun' , 600.0),
( 11 , N'Sith Alchemy' , 600.0),
( 12 , N'Throw Saber' , 600.0),
( 16 , N'Premonition' , 600.0),
( 17 , N'Comprehend Speech' , 600.0),
( 20 , N'Force Detection' , 600.0),
( 21 , N'Farseeing' , 600.0),
( 28 , N'Battle Meditation' , 600.0),
( 34 , N'Animal Bond' , 600.0),
( 35 , N'Drain Life' , 600.0),
( 36 , N'Force Lightning' , 600.0),
( 37 , N'Force Push' , 10.0),
( 38 , N'Force Breach' , 600.0)


-- Definitions for each perk
INSERT INTO dbo.Perk (ID, PerkCategoryID, CooldownCategoryID, ExecutionTypeID, IsTargetSelfOnly, Name, IsActive, Description, Enmity, EnmityAdjustmentRuleID, ForceBalanceTypeID) VALUES
 (3, 44, 2, 3, 1, 'Force Speed', 1, 'Increases movement speed and dexterity.  At higher ranks grants additional attacks.', 0, 0, 0),
 (4, 45, 4, 3, 1, 'Absorb Energy', 1, 'Absorbs a percentage of damage that the caster would take, from all sources.', 20, 2, 0),
 (5, 43, 5, 3, 1, 'Force Body', 1, 'Converts a percentage of the casters current HP into FP.', 0, 0, 0),
 (13, 45, 6, 3, 0, 'Mind Shield', 1, 'Protects the target from mind affecting powers and abilities.', 20, 2, 1),
 (19, 45, 7, 3, 1, 'Rage', 1, 'Increases STR and CON at the cost of AC and HP damage each round.  At higher ranks grants additional attacks, that do not stack with Force Speed.', 10, 2, 2),
 (76, 42, 8, 3, 0, 'Force Persuade', 1, 'Applies Domination effect to humanoid creatures with lower WIS than the caster.', 0, 0, 1),
 (78, 42, 9, 3, 0, 'Confusion', 1, 'Applies Confusion effect to organic creatures with lower WIS than the caster.', 0, 0, 1),
 (126, 42, 10, 3, 0, 'Force Stun', 1, 'Tranquilises an enemy or slows their reaction time.', 10, 1, 1),
 (173, 42, 11, 3, 0, 'Sith Alchemy', 1, 'The power to transform living (or recently-living) flesh.', 0, 0, 2),
 (174, 41, 12, 3, 0, 'Throw Saber', 1, 'The caster throws their lightsaber at nearby enemies and pulls it back to their hand.', 10, 1, 0),
 (175, 51, 16, 3, 1, 'Premonition', 1, 'The caster sees a short way into the future, allowing them to avoid an untimely fate.', 0, 0, 0),
 (176, 51, 17, 3, 1, 'Comprehend Speech', 1, 'The caster improves their ability to understand other languages.', 0, 0, 0),
 (177, 51, 20, 3, 1, 'Force Detection', 1, 'The caster senses nearby hidden creatures.', 0, 0, 0),
 (178, 46, 21, 3, 1, 'Farseeing', 1,  'The caster gets a vision of another character.', 0, 0, 0),
 (179, 46, 28, 3, 1, 'Battle Meditation', 1, 'The caster boosts their nearby allies at the expense of their own abilities.', 10, 2, 0),
 (180, 51, 34, 3, 0, 'Animal Bond', 1, 'The caster convinces a creature to travel and fight with them.', 0, 0, 0),
 (181, 42, 35, 3, 0, 'Drain Life', 1, 'Steals HP from a single target every second.', 0, 0, 2),
 (182, 42, 36, 3, 0, 'Force Lightning', 1, 'Deals electrical damage over time to a single target.', 0, 0, 2),
 (183, 49, 37, 3, 0, 'Force Push', 1, 'Knocks down a single target or, if resisted, slows the target instead.', 0, 0, 0),
 (184, 42, 38, 3, 0, 'Force Breach', 1, 'Deals direct damage to a single target.', 0, 0, 0),
 (185, 45, NULL, 3, 0, 'Force Heal', 1, 'Restores HP on a single target over time.', 0, 0, 1)
 ;

-- Levels for each perk.
ALTER TABLE dbo.PerkLevel
ADD SpecializationID INT NOT NULL DEFAULT 0
CONSTRAINT FK_PerkLevel_SpecializationID FOREIGN KEY REFERENCES dbo.Specialization(ID);

ALTER TABLE dbo.PerkLevel ADD BaseFPCost INT NOT NULL DEFAULT 0;
GO

DECLARE @PerkLevelID INT;

 INSERT INTO dbo.PerkLevel (PerkID, Level, Price, Description, SpecializationID, BaseFPCost) VALUES
 (181,1,4, 'Steals 5 HP from a single target every second.', 2, 4),
 (181,2,4, 'Steals 6 HP from a single target every second.', 2, 4),
 (181,3,5, 'Steals 7 HP from a single target every second.', 2, 4),
 (181,4,5, 'Steals 8 HP from a single target every second.', 2, 4),
 (181,5,6, 'Steals 10 HP from a single target every second.', 2, 4),
 (182,1,4, 'Damages a single target for 10 HP every second.', 2, 2),
 (182,2,4, 'Damages a single target for 12 HP every second.', 2, 2),
 (182,3,5, 'Damages a single target for 14 HP every second.', 2, 2),
 (182,4,5, 'Damages a single target for 16 HP every second.', 2, 2),
 (182,5,6, 'Damages a single target for 20 HP every second.', 2, 2),
 (183,1,2, 'Knockdown a small target. If resisted, target is slowed for 6 seconds.', 0, 4),
 (183,2,3, 'Knockdown a medium or smaller target. If resisted, target is slowed for 6 seconds.', 0, 6),
 (183,3,4, 'Knockdown a large or smaller target. If resisted, target is slowed for 6 seconds.', 3, 8),
 (183,4,5, 'Knockdown any size target. If resisted, target is slowed for 6 seconds.', 3, 10),
 (184,1,4, 'Deals 100 damage to a single target.', 2, 8),
 (184,2,5, 'Deals 125 damage to a single target.', 2, 10),
 (184,3,6, 'Deals 160 damage to a single target.', 2, 12),
 (184,4,7, 'Deals 200 damage to a single target.', 2, 14),
 (184,5,8, 'Deals 250 damage to a single target.', 2, 16),
 (185,1,2, 'Heals a single target for 2 HP every second.', 0, 1),
 (185,2,2, 'Heals a single target for 3 HP every second.', 0, 2),
 (185,3,3, 'Heals a single target for 5 HP every second.', 0, 3),
 (185,4,3, 'Heals a single target for 7 HP every second.', 2, 4),
 (185,5,4, 'Heals a single target for 10 HP every second.', 2, 5),
 (3,1,2,'Increases movement speed by 10% and Dexterity by 2.', 0, 2),
 (3,2,2,'Increases movement speed by 20% and Dexterity by 4.', 0, 4),
 (3,3,3,'Increases movement speed by 30%, Dexterity by 6 and grants an extra attack.', 0, 6),
 (3,4,3,'Increases movement speed by 40%, Dexterity by 8 and grants an extra attack.', 1, 8),
 (3,5,12,'Increases movement speed by 50%, Dexterity by 10 and grants two extra attacks and Epic Dodge.', 1, 20), 
 (4,1,2,'Grants 10% immunity to all damage while the caster retains concentration.', 0, 2),
 (4,2,2,'Grants 20% immunity to all damage while the caster retains concentration.', 0, 4),
 (4,3,3,'Grants 30% immunity to all damage while the caster retains concentration.', 0, 6),
 (4,4,3,'Grants 40% immunity to all damage while the caster retains concentration.', 2, 8),
 (4,5,4,'Grants 50% immunity to all damage while the caster retains concentration.', 2, 10),
 (5,1,3,'Converts 10% of the casters current HP into FP.', 0, 0),
 (5,2,4,'Converts 20% of the casters current HP into FP.', 0, 0),
 (5,3,5,'Converts 35% of the casters current HP into FP.', 0, 0),
 (5,4,6,'Converts 50% of the casters current HP into FP.', 0, 0),
 (13,1,4,'Immune to Tranquilisation effects while concentrating.', 0, 1),
 (13,2,6,'Immune to Tranquilisation, Confusion and Persuade effects while concentrating.', 0, 2),
 (13,3,8,'Immune to Tranquilisation, Confusion, Persuade and Drain effects while concentrating.', 1, 3),
 (19,1,2,'Increases Strength and Dexterity by 2 while concentrating but reduces AC by 2 and deals 2 damage per round.', 0, 2),
 (19,2,2,'Increases Strength and Dexterity by 4 while concentrating but reduces AC by 2 and deals 4 damage per round.', 0, 4),
 (19,3,5,'Increases Strength and Dexterity by 6 while concentrating and grants an extra attack but reduces AC by 4 and deals 6 damage per round.', 0, 6),
 (19,4,5,'Increases Strength and Dexterity by 8 while concentrating and grants an extra attack but reduces AC by 4 and deals 8 damage per round.', 1, 8),
 (19,5,8,'Increases Strength and Dexterity by 10 while concentrating and grants two extra attacks but reduces AC by 2 and deals 10 damage per round.', 1, 10),
 (76,1,7,'Applies Domination effect to a single humanoid target with lower WIS than the caster, while the caster concentrates.', 0, 8),
 (76,2,7,'Applies Domination effect to all hostile humanoid targets within 5m with lower WIS than the caster, while the caster concentrates.', 2, 20),
 (78,1,7,'Applies Confusion effect to a single non-mechanical target with lower WIS than the caster, while the caster concentrates.', 0, 8),
 (78,2,7,'Applies Confusion effect to all hostile non-mechanical targets within 10m with lower WIS than the caster, while the caster concentrates.', 2, 20),
 (126,1,4,'Single target is Tranquilised while the caster concentrates or, if resisted, gets -5 to AB and AC.', 0, 8),
 (126,2,7,'Target and nearest other enemy within 10m is Tranquilised while the caster concentrates or, if resisted, get -5 to AB and AC.', 2, 12),
 (126,3,10,'Target and all other enemies within 10 are Tranquilised while the caster concentrates or, if resisted, get -5 to AB and AC.', 2, 20),
 (173,1,0,'Unlocks Sith Alchemy.', 2, 0),
 (173,2,7,'When used on a corpse, raises the creature as a henchman while the caster concentrates.', 2, 25),
 (173,3,7,'Alchemist can create monsters.', 2, 300),
 (173,4,0,'Alchemist can employ monsters as henchmen while they concentrate.', 2, 5),
 (174,1,2,'Throw your equipped lightsaber up to 5m for (saber damage + INT modifier) * 100%.  This ability hits automatically.', 0, 4),
 (174,2,2,'Throw your equipped lightsaber up to 10m for (saber damage + INT modifier) * 125%.  This ability hits automatically.', 0, 5),
 (174,3,2,'Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 160%.  This ability hits automatically.', 0, 6),
 (174,4,2,'Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 200%.  This ability hits automatically and will chain to a second target within 5m of the first.', 1, 8),
 (174,5,2,'Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 250%.  This ability hits automatically and will chain to a second and third target within 5m each.', 1, 10),
 (175,1,4,'The next time the caster would die in the next 30 minutes, they are instead healed to 25% of their max HP.', 0, 5),
 (175,2,7,'The next time the caster would die in the next 30 minutes, they are instead healed to 50% of their max HP.', 0, 5),
 (175,3,10,'For 12s after casting, the caster is immune to all damage, and the next time the caster would die in the next 30 minutes, they are instead healed to 25% of their max HP.', 3, 16),
 (176,1,3,'The caster counts has having 5 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.', 0, 1),
 (176,2,4,'The caster counts has having 10 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.', 0, 2),
 (176,3,5,'The caster counts has having 15 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.', 3, 3),
 (176,4,6,'The caster counts has having 20 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.', 3, 4),
 (177,1,2,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 0, 1),
 (177,2,2,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 0, 2),
 (177,3,3,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 3, 3),
 (177,4,3,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 3, 4),
 (177,5,4,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 3, 5),
 (178,1,10,'The caster attempts to view what another character is currently doing.', 0, 30),
 (179,1,4, 'The caster gets -5 AB & AC but their nearby party members get +5 AB & AC', 0, 1),
 (179,2,6, 'The caster gets -10 AB & AC but their nearby party members get +10 AB & AC', 0, 2),
 (179,3,8, 'The caster and nearby enemies get -10 AB & AC but the nearby party members get +10 AB & AC', 0, 3),
 (180,1,2, 'The caster befriends an animal or beast with up to Challenge Rating 4.', 0, 4),
 (180,2,2, 'The caster befriends an animal or beast with up to Challenge Rating 8.', 0, 8),
 (180,3,3, 'The caster befriends an animal or beast with up to Challenge Rating 12.', 0, 12),
 (180,4,3, 'The caster befriends an animal or beast with up to Challenge Rating 16.', 3, 16),
 (180,5,4, 'The caster befriends an animal or beast with up to Challenge Rating 20.', 3, 20),
 (180,6,5, 'The caster befriends an animal or beast with any Challenge Rating.', 3, 30)
 ;
 
SET @PerkLevelID = SCOPE_IDENTITY();

-- Add the skill requirement for each perk level.
 INSERT INTO dbo.PerkLevelSkillRequirement (PerkLevelID, SkillID, RequiredRank) VALUES
  
  -- Drain Life
  (@PerkLevelID-83,19,30),
  (@PerkLevelID-82,19,45),
  (@PerkLevelID-81,19,60),
  (@PerkLevelID-80,19,75),
  (@PerkLevelID-79,19,90),
  
  -- Force Lightning
  (@PerkLevelID-78,19,30),
  (@PerkLevelID-77,19,45),
  (@PerkLevelID-76,19,60),
  (@PerkLevelID-75,19,75),
  (@PerkLevelID-74,19,90),

  -- Force Push
  (@PerkLevelID-73,19,0),
  (@PerkLevelID-72,19,10),
  (@PerkLevelID-71,19,20),
  (@PerkLevelID-70,19,30),

  -- Force Breach
  (@PerkLevelID-69,19,50),
  (@PerkLevelID-68,19,60),
  (@PerkLevelID-67,19,70),
  (@PerkLevelID-66,19,80),
  (@PerkLevelID-65,19,90),

  -- Force Heal
  (@PerkLevelID-64,20,0),
  (@PerkLevelID-63,20,10),
  (@PerkLevelID-62,20,20),
  (@PerkLevelID-61,20,30),
  (@PerkLevelID-60,20,40),

  -- Force Speed
  (@PerkLevelID-59,20,0),
  (@PerkLevelID-58,20,10),
  (@PerkLevelID-57,20,25),
  (@PerkLevelID-56,20,40),
  (@PerkLevelID-55,20,80),

  -- Absorb Energy
  (@PerkLevelID-54,20,0),
  (@PerkLevelID-53,20,15),
  (@PerkLevelID-52,20,30),
  (@PerkLevelID-51,20,45),
  (@PerkLevelID-50,20,60),

  -- Force Body
  (@PerkLevelID-49,20,10),
  (@PerkLevelID-48,20,25),
  (@PerkLevelID-47,20,40),
  (@PerkLevelID-46,20,55),

  -- Mind Shield
  (@PerkLevelID-45,20,10),
  (@PerkLevelID-44,20,30),
  (@PerkLevelID-43,20,50),
  
  -- Rage
  (@PerkLevelID-42,20,10),
  (@PerkLevelID-41,20,30),
  (@PerkLevelID-40,20,50),
  (@PerkLevelID-39,20,70),
  (@PerkLevelID-38,20,90),

  -- Force Persuade
  (@PerkLevelID-37,19,40),
  (@PerkLevelID-36,19,80),

  -- Confusion
  (@PerkLevelID-35,19,40),
  (@PerkLevelID-34,19,80),

  -- Force Stun
  (@PerkLevelID-33,19,10),
  (@PerkLevelID-32,19,50),
  (@PerkLevelID-31,19,80),

  -- Sith Alchemy
  (@PerkLevelID-30,19,0),
  (@PerkLevelID-29,19,80),
  (@PerkLevelID-28,19,90),
  (@PerkLevelID-27,19,90),

  -- Throw Saber
  (@PerkLevelID-26,19,10),
  (@PerkLevelID-25,19,20),
  (@PerkLevelID-24,19,30),
  (@PerkLevelID-23,19,40),
  (@PerkLevelID-22,19,50),

  -- Premonition
  (@PerkLevelID-21,21,0),
  (@PerkLevelID-20,21,20),
  (@PerkLevelID-19,21,50),

  -- Comprehend Speech
  (@PerkLevelID-18,21,0),
  (@PerkLevelID-17,21,15),
  (@PerkLevelID-16,21,30),
  (@PerkLevelID-15,21,45),

  -- Force Detection
  (@PerkLevelID-14,21,0),
  (@PerkLevelID-13,21,5),
  (@PerkLevelID-12,21,20),
  (@PerkLevelID-11,21,35),
  (@PerkLevelID-10,21,50),

  -- Farseeing
  (@PerkLevelID-9,21,80),

  -- Battle Meditation
  (@PerkLevelID-8,21,40),
  (@PerkLevelID-7,21,60),
  (@PerkLevelID-6,21,80),

  -- Animal Bond
  (@PerkLevelID-5,21,10),
  (@PerkLevelID-4,21,25),
  (@PerkLevelID-3,21,40),
  (@PerkLevelID-2,21,55),
  (@PerkLevelID-1,21,70),
  (@PerkLevelID, 21, 85)
  ;
 
INSERT INTO dbo.Quest ( ID ,
                        Name ,
                        JournalTag ,
                        FameRegionID ,
                        RequiredFameAmount ,
                        AllowRewardSelection ,
                        RewardGold ,
                        RewardKeyItemID ,
                        RewardFame ,
                        IsRepeatable ,
                        MapNoteTag ,
                        StartKeyItemID ,
                        RemoveStartKeyItemAfterCompletion ,
                        OnAcceptRule ,
                        OnAdvanceRule ,
                        OnCompleteRule ,
                        OnKillTargetRule ,
                        OnAcceptArgs ,
                        OnAdvanceArgs ,
                        OnCompleteArgs ,
                        OnKillTargetArgs )
VALUES ( 99 ,    -- ID - int
         N'Sith Alchemy' ,  -- Name - nvarchar(100)
         N'' ,  -- JournalTag - nvarchar(32)
         1 ,    -- FameRegionID - int
         0 ,    -- RequiredFameAmount - int
         0 , -- AllowRewardSelection - bit
         0 ,    -- RewardGold - int
         NULL ,    -- RewardKeyItemID - int
         0 ,    -- RewardFame - int
         0 , -- IsRepeatable - bit
         N'' ,  -- MapNoteTag - nvarchar(32)
         NULL ,    -- StartKeyItemID - int
         0 , -- RemoveStartKeyItemAfterCompletion - bit
         N'' ,  -- OnAcceptRule - nvarchar(32)
         N'' ,  -- OnAdvanceRule - nvarchar(32)
         N'' ,  -- OnCompleteRule - nvarchar(32)
         N'' ,  -- OnKillTargetRule - nvarchar(32)
         N'' ,  -- OnAcceptArgs - nvarchar(256)
         N'' ,  -- OnAdvanceArgs - nvarchar(256)
         N'' ,  -- OnCompleteArgs - nvarchar(256)
         N''    -- OnKillTargetArgs - nvarchar(256)
    )

-- Set the Sith Alchemy perk level 1 to be gated on the above quest.
INSERT INTO dbo.PerkLevelQuestRequirement (PerkLevelID, RequiredQuestID) VALUES (@PerkLevelID-30, 99);



INSERT INTO dbo.PerkFeat ( PerkID , FeatID , PerkLevelUnlocked ) VALUES 
-- Force Speed
(3, 1165, 1),
(3, 1166, 2),
(3, 1167, 3),
(3, 1168, 4),
(3, 1169, 5),

-- Absorb Energy
(4, 1170, 1),
(4, 1171, 2),
(4, 1172, 3),
(4, 1173, 4),
(4, 1174, 5),

-- Force Heal
(185, 1175, 1),
(185, 1176, 2),
(185, 1177, 3),
(185, 1178, 4),
(185, 1179, 5),

-- Force Body
(5, 1180, 1),
(5, 1181, 2),
(5, 1182, 3),
(5, 1183, 4),

-- Mind Shield
(13, 1184, 1),
(13, 1185, 2),
(13, 1186, 3),

-- Rage
(19, 1187, 1),
(19, 1188, 2),
(19, 1189, 3),
(19, 1190, 4),
(19, 1191, 5),

-- Force Persuade
(76, 1192, 1),
(76, 1193, 2),

-- Confusion
(78, 1194, 1),
(78, 1195, 2),

-- Force Stun
(126, 1196, 1),
(126, 1197, 2),
(126, 1198, 3),

-- Force Push
(183, 1199, 1),
(183, 1200, 2),
(183, 1201, 3),
(183, 1202, 4),

-- Force Lightning
(182, 1203, 1),
(182, 1204, 2),
(182, 1205, 3),
(182, 1206, 4),
(182, 1207, 5),

-- Drain Life
(181, 1208, 1),
(181, 1209, 2),
(181, 1210, 3),
(181, 1211, 4),
(181, 1212, 5),

-- Sith Alchemy
(173, 1213, 1),
(173, 1214, 2),
(173, 1215, 3),

-- Force Breach
(184, 1216, 1),
(184, 1217, 2),
(184, 1218, 3),
(184, 1219, 4),
(184, 1220, 5),

-- Throw Saber
(174, 1221, 1),
(174, 1222, 2),
(174, 1223, 3),
(174, 1224, 4),
(174, 1225, 5),

-- Premonition
(175, 1226, 1),
(175, 1227, 2),
(175, 1228, 3),

-- Comprehend Speech
(176, 1229, 1),
(176, 1230, 2),
(176, 1231, 3),
(176, 1232, 4),

-- Force Detection
(177, 1233, 1),
(177, 1234, 2),
(177, 1235, 3),
(177, 1236, 4),
(177, 1237, 5),

-- Battle Meditation
(179, 1238, 1),
(179, 1239, 2),
(179, 1240, 3),

-- Animal Bond
(180, 1241, 1),
(180, 1242, 2),
(180, 1243, 3),
(180, 1244, 4),
(180, 1245, 5),
(180, 1246, 6)



-- Remove custom effects

-- Remove Chainspell
DELETE FROM dbo.CustomEffect
WHERE ID = 6



-- New perk execution types
INSERT INTO dbo.PerkExecutionType ( ID ,
                                    Name )
VALUES ( 7 , -- ID - int
         N'Concentration Ability' -- Name - nvarchar(32)
    )


-- Mark concentration execution type
UPDATE dbo.Perk
SET ExecutionTypeID = 7
WHERE ID IN (4, 13, 19, 76, 126, 173, 176, 178, 179, 180, 181, 182, 185)



-- Add the active concentration perk ID column to player table
ALTER TABLE dbo.Player
ADD ActiveConcentrationPerkID INT NULL

-- Add a FK constraint to this new column
ALTER TABLE dbo.Player
ADD CONSTRAINT FK_Player_ActiveConcentrationPerkID
FOREIGN KEY(ActiveConcentrationPerkID) REFERENCES Perk(ID)

-- Add the active concentration perk level column to player table
ALTER TABLE dbo.Player
ADD ActiveConcentrationTier INT NOT NULL DEFAULT 0


-- Remove BaseFPCost from the perk. It's now on the PerkLevel table instead.
EXEC dbo.ADM_Drop_Column @TableName = N'Perk' , -- nvarchar(200)
                         @ColumnName = N'BaseFPCost'  -- nvarchar(200)


-- Add a unique constraint to ensure we never get more than one feat per perk ID and perk level unlocked
ALTER TABLE dbo.PerkFeat
ADD CONSTRAINT UQ_PerkFeat_SurrogateKey UNIQUE(PerkID, PerkLevelUnlocked)

-- Safety constraint to ensure we only ever have one feat ID registered in the PerkFeat table.
-- A lot of our look-ups are based on the feat only, so we want to be sure there are no dupes.
ALTER TABLE dbo.PerkFeat
ADD CONSTRAINT UQ_PerkFeat_FeatID UNIQUE(FeatID)



-- Moving FP costs and tick interval to the perk feat instead of perk level
ALTER TABLE dbo.PerkFeat
ADD BaseFPCost INT NOT NULL DEFAULT 0

ALTER TABLE dbo.PerkFeat
ADD ConcentrationFPCost INT NOT NULL DEFAULT 0

ALTER TABLE dbo.PerkFeat
ADD ConcentrationTickInterval INT NOT NULL DEFAULT 0


EXEC dbo.ADM_Drop_Column @TableName = N'PerkLevel' , -- nvarchar(200)
                         @ColumnName = N'BaseFPCost'  -- nvarchar(200)
GO


DECLARE @PerkID INT 

-- 3 = Force Speed
SET @PerkID = 3

UPDATE dbo.PerkFeat
SET BaseFPCost = 2,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 4,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 6,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 20,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 5


	
-- 4 = Absorb Energy
SET @PerkID = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 2,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 4,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 6,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 10,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5



-- 185 = Force Heal
SET @PerkID = 185
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5


	
-- 13 = Mind Shield
SET @PerkID = 13
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 3,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3


-- 19 = Rage
SET @PerkID = 19
UPDATE dbo.PerkFeat
SET BaseFPCost = 2,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 4,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 6,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 10,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5



-- 76 = Force Persuade
SET @PerkID = 76
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 20,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2


	
-- 78 = Confusion
SET @PerkID = 78
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 20,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2



-- 126 = Force Stun
SET @PerkID = 126
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 12,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 20,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3


	
-- 183 = Force Push
SET @PerkID = 183
UPDATE dbo.PerkFeat
SET BaseFPCost = 4,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 6,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 10,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4


	
-- 182 = Force Lightning
SET @PerkID = 182
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5


	
-- 181 = Drain Life
SET @PerkID = 181
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 4,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 4,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 4,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 4,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 4,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5


	
-- 173 = Sith Alchemy
SET @PerkID = 173
UPDATE dbo.PerkFeat
SET BaseFPCost = 25,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 300,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 5,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3


	
-- 184 = Force Breach
SET @PerkID = 184
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 10,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 12,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 14,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 16,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5




-- 174 = Throw Saber
SET @PerkID = 174
UPDATE dbo.PerkFeat
SET BaseFPCost = 4,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 5,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 6,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 10,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5

-- 175 = Premonition
SET @PerkID = 174
UPDATE dbo.PerkFeat
SET BaseFPCost = 5,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 5,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 16,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3



-- 176 = Comprehend Speech
SET @PerkID = 176
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 3,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 4,
	ConcentrationTickInterval = 6
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4


	
-- 177 = Force Detection
SET @PerkID = 177
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 3,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 4,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 5,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5



-- 178 = Farseeing
SET @PerkID = 178
UPDATE dbo.PerkFeat
SET BaseFPCost = 30,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1

	
-- 179 = Battle Meditation
SET @PerkID = 179
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 1,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 2,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 0,
	ConcentrationFPCost = 3,
	ConcentrationTickInterval = 1
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3

	
-- 180 = Animal Bond
SET @PerkID = 180
UPDATE dbo.PerkFeat
SET BaseFPCost = 4,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID
	AND PerkLevelUnlocked = 1
UPDATE dbo.PerkFeat
SET BaseFPCost = 8,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 2
UPDATE dbo.PerkFeat
SET BaseFPCost = 12,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 3
UPDATE dbo.PerkFeat
SET BaseFPCost = 16,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 4
UPDATE dbo.PerkFeat
SET BaseFPCost = 20,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 5
UPDATE dbo.PerkFeat
SET BaseFPCost = 30,
	ConcentrationFPCost = 0,
	ConcentrationTickInterval = 0
WHERE PerkID = @PerkID 
	AND PerkLevelUnlocked = 6




-- Some perks are cut until phase 2. Disable them for now.
UPDATE dbo.Perk
SET IsActive = 0
WHERE ID IN (
	178,
	177,
	180,
	175,
	76
)


-- Add module version to server configuration table.
-- This will get bumped to 1 in the ModuleMigrationService
ALTER TABLE dbo.ServerConfiguration
ADD ModuleVersion INT NOT NULL DEFAULT 0
GO

-- Refund all weapon proficiency perks. Then disable them from being purchased.
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 38
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 113
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 114
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 115
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 116
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 117
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 118
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 119
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 120
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 121
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 122
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 129


UPDATE dbo.Perk
SET IsActive = 0
WHERE ID IN (38 ,113,114,115,116,117,118,119,120,121,122,129)



-- Make a loot table specifically for dropping reassembly fuel cells.
INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 54 , -- ID - int
         N'Reassembly Fuel Cell' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 54 ,    -- LootTableID - int
         'ass_power' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )


-- Add reassembly fuel cells to existing space encounter loot tables

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 51 ,    -- LootTableID - int
         'ass_power' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         1 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 52 ,    -- LootTableID - int
         'ass_power' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         1 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 53 ,    -- LootTableID - int
         'ass_power' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         1 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

-- Rename lightsaber to training foil
UPDATE dbo.CraftBlueprint
SET ItemName = REPLACE(ItemName, 'Lightsaber', 'Training Foil')
WHERE ID IN (
211,212,213,214,215,
612,613,614,615,616,
622,623,624,625,626,
632,633,634,635,636)

-- Rename saberstaff to lightfoil staff
UPDATE dbo.CraftBlueprint
SET ItemName = REPLACE(ItemName, 'Saberstaff', 'Training Foil Staff')
WHERE ID IN (
216,217,218,219,220,
617,618,619,620,621,
627,628,629,630,631,
637,638,639,640,641
)



-- Add Shield Proficiency perk
INSERT INTO dbo.Perk ( ID ,
                       Name ,
                       IsActive ,
                       BaseCastingTime ,
                       Description ,
                       PerkCategoryID ,
                       CooldownCategoryID ,
                       ExecutionTypeID ,
                       IsTargetSelfOnly ,
                       Enmity ,
                       EnmityAdjustmentRuleID ,
                       CastAnimationID ,
                       ForceBalanceTypeID )
VALUES ( 172 ,    -- ID - int
         'Shield Proficiency' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         0.0 ,  -- BaseCastingTime - float
         N'Increases your damage reduction by 2% while equipped with a shield.' ,  -- Description - nvarchar(256)
         6 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL ,    -- CastAnimationID - int
         0      -- ForceBalanceTypeID - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 172 ,   -- PerkID - int
         1 ,   -- Level - int
         3 ,   -- Price - int
         N'2% damage reduction' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         9 , -- SkillID - int
         10   -- RequiredRank - int
    )
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 172 ,   -- PerkID - int
         2 ,   -- Level - int
         3 ,   -- Price - int
         N'4% damage reduction' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         9 , -- SkillID - int
         20   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 172 ,   -- PerkID - int
         3 ,   -- Level - int
         3 ,   -- Price - int
         N'6% damage reduction' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         9 , -- SkillID - int
         30   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 172 ,   -- PerkID - int
         4 ,   -- Level - int
         3 ,   -- Price - int
         N'8% damage reduction' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         9 , -- SkillID - int
         40   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 172 ,   -- PerkID - int
         5 ,   -- Level - int
         3 ,   -- Price - int
         N'10% damage reduction' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         9 , -- SkillID - int
         50   -- RequiredRank - int
    )


GO


-- This table is redundant because all skills use the same XP requirements. If they didn't, skill decay wouldn't
-- work properly. I've changed the code to read XP requirements from a static dictionary instead of pinging
-- the DB/cache. This table is no longer necessary and will be dropped.
DROP TABLE dbo.SkillXPRequirement

GO



CREATE TABLE Guild (
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(64) NOT NULL,
	Description NVARCHAR(1000) NOT NULL
)


INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 1 ,   -- ID - int
         N'Hunter''s Guild' , -- Name - nvarchar(64)
         N'Specializes in the detection and removal of threats across the galaxy.'   -- Description - nvarchar(64)
    )

INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 2 ,   -- ID - int
         N'Engineering Guild' , -- Name - nvarchar(64)
         N'Specializes in the construction of engineering and electronic items.'   -- Description - nvarchar(64)
    )

INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 3 ,   -- ID - int
         N'Weaponsmith Guild' , -- Name - nvarchar(64)
         N'Specializes in the construction of weaponry.'   -- Description - nvarchar(64)
    )

INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 4 ,   -- ID - int
         N'Armorsmith Guild' , -- Name - nvarchar(64)
         N'Specializes in the construction of armor.'   -- Description - nvarchar(64)
    )
	
	
CREATE TABLE PCGuildPoint(
	ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	PlayerID UNIQUEIDENTIFIER NOT NULL,
	GuildID INT NOT NULL,
	Rank INT NOT NULL,
	Points INT NOT NULL,

	CONSTRAINT FK_PCGuildPoint_PlayerID FOREIGN KEY(PlayerID)
		REFERENCES dbo.Player(ID),
	CONSTRAINT FK_PCGuildPoint_GuildID FOREIGN KEY(GuildID)
		REFERENCES dbo.Guild(ID),
	CONSTRAINT UQ_PCGuildPoint_PlayerIDGuildID UNIQUE(PlayerID, GuildID)
)
GO




SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
-- =============================================
-- Author:		zunath
-- Create date: 2018-11-18
-- Description:	Retrieve all associated player data for use in caching.
-- Updated 2018-11-27: Retrieve BankItem records
-- Updated 2018-12-14: Retrieve PCSkillPool records
-- Updated 2019-03-11: Exclude impounded items which have been retrieved. Exclude impounded items
--					   which have not been retrieved after 30 days.
-- Updated 2019-03-31: Change the excluded impound items query to cast to DATE to help improve performance.
--					   According to various sources, DATEADD on a DATE is quicker than a DATETIME2
-- Updated 2019-06-09: Retrieve PCGuildPoint data.
-- =============================================
ALTER PROCEDURE [dbo].[GetPlayerData]
	@PlayerID UNIQUEIDENTIFIER
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
    -- Get PC cooldowns
	SELECT ID ,
           PlayerID ,
           CooldownCategoryID ,
           DateUnlocked 
	FROM dbo.PCCooldown
	WHERE PlayerID = @PlayerID
	
	
	-- Get PC crafted blueprints
	SELECT ID ,
           PlayerID ,
           CraftBlueprintID ,
           DateFirstCrafted 
	FROM dbo.PCCraftedBlueprint  
	WHERE PlayerID = @PlayerID
	-- Get PC Custom Effects
	SELECT ID ,
           PlayerID ,
           CustomEffectID ,
           Ticks ,
           EffectiveLevel ,
           Data ,
           CasterNWNObjectID ,
           StancePerkID 
	FROM dbo.PCCustomEffect 
	WHERE PlayerID = @PlayerID
	-- Get PC Impounded Items
	SELECT ID ,
           PlayerID ,
           ItemName ,
           ItemTag ,
           ItemResref ,
           ItemObject ,
           DateImpounded ,
           DateRetrieved 
	FROM dbo.PCImpoundedItem 
	WHERE PlayerID = @PlayerID
		AND DateRetrieved IS NULL 
		AND GETUTCDATE() < DATEADD(DAY, 30, CAST(DateImpounded AS DATE))
	-- Get PC Key Items
	SELECT ID ,
           PlayerID ,
           KeyItemID ,
           AcquiredDate 
	FROM dbo.PCKeyItem 
	WHERE PlayerID = @PlayerID
	-- Get PC Map Pin
	SELECT ID ,
           PlayerID ,
           AreaTag ,
           PositionX ,
           PositionY ,
           NoteText 
	FROM dbo.PCMapPin
	WHERE PlayerID = @PlayerID 
	-- Get PC Map Progression
	SELECT ID ,
           PlayerID ,
           AreaResref ,
           Progression 
	FROM dbo.PCMapProgression
	WHERE PlayerID = @PlayerID
	
	
	 -- Get PC Object Visibility
	 SELECT ID ,
            PlayerID ,
            VisibilityObjectID ,
            IsVisible 
	 FROM dbo.PCObjectVisibility 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Outfit
	 SELECT PlayerID ,
            Outfit1 ,
            Outfit2 ,
            Outfit3 ,
            Outfit4 ,
            Outfit5 ,
            Outfit6 ,
            Outfit7 ,
            Outfit8 ,
            Outfit9 ,
            Outfit10 
	 FROM dbo.PCOutfit
	 WHERE PlayerID = @PlayerID 
	 -- Get PC Overflow Items
	 SELECT ID ,
            PlayerID ,
            ItemName ,
            ItemTag ,
            ItemResref ,
            ItemObject 
	 FROM dbo.PCOverflowItem 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Perks
	 SELECT ID ,
            PlayerID ,
            AcquiredDate ,
            PerkID ,
            PerkLevel 
	 FROM dbo.PCPerk
	 WHERE PlayerID = @PlayerID 
	 -- Get PC Quest Item Progress
	 SELECT ID ,
            PlayerID ,
            PCQuestStatusID ,
            Resref ,
            Remaining ,
            MustBeCraftedByPlayer 
	 FROM dbo.PCQuestItemProgress 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Kill Target Progress
	 SELECT ID ,
            PlayerID ,
            PCQuestStatusID ,
            NPCGroupID ,
            RemainingToKill 
	 FROM dbo.PCQuestKillTargetProgress
	 WHERE PlayerID = @PlayerID 
	 -- Get PC Quest Status
	 SELECT ID ,
            PlayerID ,
            QuestID ,
            CurrentQuestStateID ,
            CompletionDate ,
            SelectedItemRewardID 
	 FROM dbo.PCQuestStatus 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Regional Fame
	 SELECT ID ,
            PlayerID ,
            FameRegionID ,
            Amount 
	 FROM dbo.PCRegionalFame 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Search Sites
	 SELECT ID ,
            PlayerID ,
            SearchSiteID ,
            UnlockDateTime 
	 FROM dbo.PCSearchSite 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Search Site Items
	 SELECT ID ,
            PlayerID ,
            SearchSiteID ,
            SearchItem
	 FROM dbo.PCSearchSiteItem 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Skills
	 SELECT ID ,
            PlayerID ,
            SkillID ,
            XP ,
            Rank ,
            IsLocked 
	 FROM dbo.PCSkill 
	 WHERE PlayerID = @PlayerID
	 -- Get Bank Items
	 SELECT bi.ID ,
            bi.BankID ,
            bi.PlayerID ,
            bi.ItemID ,
            bi.ItemName ,
            bi.ItemTag ,
            bi.ItemResref ,
            bi.ItemObject ,
            bi.DateStored 
	 FROM dbo.BankItem bi
	 WHERE bi.PlayerID = @PlayerID 
	 -- Get PC Skill Pools
	 SELECT pcsp.ID ,
            pcsp.PlayerID ,
            pcsp.SkillCategoryID ,
            pcsp.Levels 
	 FROM dbo.PCSkillPool pcsp
	 WHERE pcsp.PlayerID = @PlayerID 
	 -- Get PC Guild Point data
	 SELECT ID ,
            PlayerID ,
            GuildID ,
            Rank ,
            Points 
	 FROM PCGuildPoint
	 WHERE PlayerID = @PlayerID
END
GO

ALTER TABLE dbo.PCQuestStatus
ADD TimesCompleted INT NOT NULL DEFAULT 0
GO

UPDATE dbo.PCQuestStatus
SET TimesCompleted = 1
WHERE CompletionDate IS NOT NULL


ALTER TABLE dbo.Quest
ADD RewardGuildID INT NULL
CONSTRAINT FK_Quest_RewardGuildID FOREIGN KEY REFERENCES dbo.Guild(ID)

ALTER TABLE dbo.Quest
ADD RewardGuildPoints INT NOT NULL DEFAULT 0


ALTER TABLE dbo.ServerConfiguration
ADD LastGuildTaskUpdate DATETIME2 NOT NULL DEFAULT '1900-01-01'

CREATE TABLE GuildTask(
	ID INT PRIMARY KEY NOT NULL IDENTITY,
	GuildID INT NOT NULL,
	QuestID INT NOT NULL,
	RequiredRank INT NOT NULL,
	IsCurrentlyOffered BIT NOT NULL,

	CONSTRAINT FK_GuildTask_GuildID FOREIGN KEY(GuildID)
		REFERENCES Guild(ID),
	CONSTRAINT FK_GuildTask_QuestID FOREIGN KEY(QuestID)
		REFERENCES dbo.Quest(ID)
)
GO


/*
The following query was made to generate the following insert statements. 


SELECT 
	-- Begin quest insert
	'INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (' 
	+ CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000)) + ', '  -- ID
	+ '''Armorsmith Guild Task: 1x ' + ItemName + ''', ' -- Name
	+ '''arm_tsk_' + CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000))  + ''', ' -- JournalTag
	+ '1, ' -- FameRegionID
	+ '0, ' -- RequiredFameAmount
	+ '0, ' -- AllowRewardSelection
	+ CAST(50 * RequiredPerkLevel + (MainMinimum * 20 + SecondaryMinimum * 15 + TertiaryMinimum * 10) AS NVARCHAR(1000)) + ', ' -- RewardGold
	+ 'NULL, ' -- RewardKeyItem
	+ '0, ' -- RewardFame
	+ '1, ' -- IsRepeatable
	+ ''''',' -- MapNoteTag
	+ 'NULL, ' -- StartKeyItemID
	+ '0, ' -- RemoveStartKeyItemAfterCompletion
	+ ''''',' -- OnAcceptRule
	+ ''''',' -- OnAdvanceRule
	+ ''''',' -- OnCompleteRule
	+ ''''',' -- OnKillTargetRule
	+ ''''',' -- OnAcceptArgs
	+ ''''',' -- OnAdvanceArgs
	+ ''''',' -- OnCompleteArgs
	+ ''''',' -- OnKillTargetArgs
	+ '1, ' -- RewardGuildID
	+ CAST(10 * RequiredPerkLevel + (MainMinimum * 5 + SecondaryMinimum * 4 + TertiaryMinimum * 3) AS NVARCHAR(1000)) -- RewardGuildPoints
	+ ');', 
	-- End Quest Insert

	-- Begin quest state + required item insert
	'INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES ('
	+ CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000)) + ', '  -- QuestID
	+ '1, ' -- Sequence
	+ '4, ' -- QuestTypeID
	+ '1' -- JournalStateID
	+ '); '
	+ 'INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES ('
	+ CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000)) + ', '  -- QuestID
	+ '''' + ItemResref + ''', ' -- Resref
	+ '1, ' -- Quantity
	+ 'SCOPE_IDENTITY(), ' -- QuestStateID (SCOPE_IDENTITY() of the previous QuestState insert
	+ '1' -- MustBeCraftedByPlayer
	+ ');'

	-- End quest state + required item insert

	-- Begin GuildTask insert
	, 'INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES ('
	+ '1, ' -- GuildID
	+ CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000)) + ', ' -- QuestID
	+ CASE RequiredPerkLevel
		WHEN 0 THEN '0'
		WHEN 1 THEN '1'
		WHEN 3 THEN '2'
		WHEN 5 THEN '3'
		WHEN 7 THEN '4'
		ELSE '99'
	  END + ', '  -- RequiredRank
	+ '0' -- IsCurrentlyOffered
	+ ');'
	-- End GuildTask insert
FROM dbo.CraftBlueprint 
WHERE CraftDeviceID = 1
ORDER BY RequiredPerkLevel ASC, ItemName





*/


-- ARMORSMITH INSERTS BEGIN

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (101, 'Armorsmith Guild Task: 1x Basic Breastplate', 'arm_tsk_101', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (102, 'Armorsmith Guild Task: 1x Basic Force Boots', 'arm_tsk_102', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (103, 'Armorsmith Guild Task: 1x Basic Force Helmet', 'arm_tsk_103', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (104, 'Armorsmith Guild Task: 1x Basic Force Robes', 'arm_tsk_104', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (105, 'Armorsmith Guild Task: 1x Basic Heavy Boots', 'arm_tsk_105', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (106, 'Armorsmith Guild Task: 1x Basic Heavy Helmet', 'arm_tsk_106', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (107, 'Armorsmith Guild Task: 1x Basic Large Shield', 'arm_tsk_107', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (108, 'Armorsmith Guild Task: 1x Basic Leather Tunic', 'arm_tsk_108', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (109, 'Armorsmith Guild Task: 1x Basic Light Boots', 'arm_tsk_109', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (110, 'Armorsmith Guild Task: 1x Basic Light Helmet', 'arm_tsk_110', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (111, 'Armorsmith Guild Task: 1x Basic Power Glove', 'arm_tsk_111', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (112, 'Armorsmith Guild Task: 1x Basic Small Shield', 'arm_tsk_112', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (113, 'Armorsmith Guild Task: 1x Basic Tower Shield', 'arm_tsk_113', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 18);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (114, 'Armorsmith Guild Task: 1x Fiberplast Padding', 'arm_tsk_114', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (115, 'Armorsmith Guild Task: 1x Force Armor Core', 'arm_tsk_115', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (116, 'Armorsmith Guild Task: 1x Force Armor Segment', 'arm_tsk_116', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (117, 'Armorsmith Guild Task: 1x Heavy Armor Core', 'arm_tsk_117', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (118, 'Armorsmith Guild Task: 1x Heavy Armor Segment', 'arm_tsk_118', 1, 0, 0, 80, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 20);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (119, 'Armorsmith Guild Task: 1x Light Armor Core', 'arm_tsk_119', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (120, 'Armorsmith Guild Task: 1x Light Armor Segment', 'arm_tsk_120', 1, 0, 0, 40, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 10);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (121, 'Armorsmith Guild Task: 1x Metal Reinforcement', 'arm_tsk_121', 1, 0, 0, 55, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 14);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (122, 'Armorsmith Guild Task: 1x Breastplate I', 'arm_tsk_122', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (123, 'Armorsmith Guild Task: 1x Force Armor Repair Kit I', 'arm_tsk_123', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (124, 'Armorsmith Guild Task: 1x Force Belt I', 'arm_tsk_124', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (125, 'Armorsmith Guild Task: 1x Force Boots I', 'arm_tsk_125', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (126, 'Armorsmith Guild Task: 1x Force Helmet I', 'arm_tsk_126', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (127, 'Armorsmith Guild Task: 1x Force Necklace I', 'arm_tsk_127', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (128, 'Armorsmith Guild Task: 1x Force Robes I', 'arm_tsk_128', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (129, 'Armorsmith Guild Task: 1x Heavy Armor Repair Kit I', 'arm_tsk_129', 1, 0, 0, 155, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 37);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (130, 'Armorsmith Guild Task: 1x Heavy Belt I', 'arm_tsk_130', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (131, 'Armorsmith Guild Task: 1x Heavy Boots I', 'arm_tsk_131', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (132, 'Armorsmith Guild Task: 1x Heavy Crest I', 'arm_tsk_132', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (133, 'Armorsmith Guild Task: 1x Heavy Helmet I', 'arm_tsk_133', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (134, 'Armorsmith Guild Task: 1x Large Shield I', 'arm_tsk_134', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (135, 'Armorsmith Guild Task: 1x Leather Tunic I', 'arm_tsk_135', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (136, 'Armorsmith Guild Task: 1x Light Armor Repair Kit I', 'arm_tsk_136', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (137, 'Armorsmith Guild Task: 1x Light Belt I', 'arm_tsk_137', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (138, 'Armorsmith Guild Task: 1x Light Boots I', 'arm_tsk_138', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (139, 'Armorsmith Guild Task: 1x Light Choker I', 'arm_tsk_139', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (140, 'Armorsmith Guild Task: 1x Light Helmet I', 'arm_tsk_140', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (141, 'Armorsmith Guild Task: 1x Power Glove I', 'arm_tsk_141', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (142, 'Armorsmith Guild Task: 1x Shield Repair Kit I', 'arm_tsk_142', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (143, 'Armorsmith Guild Task: 1x Small Shield I', 'arm_tsk_143', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (144, 'Armorsmith Guild Task: 1x Tower Shield I', 'arm_tsk_144', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (145, 'Armorsmith Guild Task: 1x Breastplate II', 'arm_tsk_145', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (146, 'Armorsmith Guild Task: 1x Force Armor Repair Kit II', 'arm_tsk_146', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (147, 'Armorsmith Guild Task: 1x Force Belt II', 'arm_tsk_147', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (148, 'Armorsmith Guild Task: 1x Force Boots II', 'arm_tsk_148', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (149, 'Armorsmith Guild Task: 1x Force Helmet II', 'arm_tsk_149', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (150, 'Armorsmith Guild Task: 1x Force Necklace II', 'arm_tsk_150', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (151, 'Armorsmith Guild Task: 1x Force Robes II', 'arm_tsk_151', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (152, 'Armorsmith Guild Task: 1x Heavy Armor Repair Kit II', 'arm_tsk_152', 1, 0, 0, 255, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 57);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (153, 'Armorsmith Guild Task: 1x Heavy Belt II', 'arm_tsk_153', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (154, 'Armorsmith Guild Task: 1x Heavy Boots II', 'arm_tsk_154', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (155, 'Armorsmith Guild Task: 1x Heavy Crest II', 'arm_tsk_155', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (156, 'Armorsmith Guild Task: 1x Heavy Helmet II', 'arm_tsk_156', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (157, 'Armorsmith Guild Task: 1x Heavy Helmet III', 'arm_tsk_157', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (158, 'Armorsmith Guild Task: 1x Large Shield II', 'arm_tsk_158', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (159, 'Armorsmith Guild Task: 1x Leather Tunic II', 'arm_tsk_159', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (160, 'Armorsmith Guild Task: 1x Light Armor Repair Kit II', 'arm_tsk_160', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (161, 'Armorsmith Guild Task: 1x Light Belt II', 'arm_tsk_161', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (162, 'Armorsmith Guild Task: 1x Light Boots II', 'arm_tsk_162', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (163, 'Armorsmith Guild Task: 1x Light Choker II', 'arm_tsk_163', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (164, 'Armorsmith Guild Task: 1x Light Helmet II', 'arm_tsk_164', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (165, 'Armorsmith Guild Task: 1x Power Glove II', 'arm_tsk_165', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (166, 'Armorsmith Guild Task: 1x Shield Repair Kit II', 'arm_tsk_166', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (167, 'Armorsmith Guild Task: 1x Small Shield II', 'arm_tsk_167', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (168, 'Armorsmith Guild Task: 1x Tower Shield II', 'arm_tsk_168', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (169, 'Armorsmith Guild Task: 1x Additional Fuel Tank (Small)', 'arm_tsk_169', 1, 0, 0, 305, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 64);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (170, 'Armorsmith Guild Task: 1x Additional Stronidium Tank (Small)', 'arm_tsk_170', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (171, 'Armorsmith Guild Task: 1x Breastplate III', 'arm_tsk_171', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (172, 'Armorsmith Guild Task: 1x Force Armor Repair Kit III', 'arm_tsk_172', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (173, 'Armorsmith Guild Task: 1x Force Belt III', 'arm_tsk_173', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (174, 'Armorsmith Guild Task: 1x Force Boots III', 'arm_tsk_174', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (175, 'Armorsmith Guild Task: 1x Force Helmet III', 'arm_tsk_175', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (176, 'Armorsmith Guild Task: 1x Force Necklace III', 'arm_tsk_176', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (177, 'Armorsmith Guild Task: 1x Force Robes III', 'arm_tsk_177', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (178, 'Armorsmith Guild Task: 1x Heavy Armor Repair Kit III', 'arm_tsk_178', 1, 0, 0, 355, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 77);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (179, 'Armorsmith Guild Task: 1x Heavy Belt III', 'arm_tsk_179', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (180, 'Armorsmith Guild Task: 1x Heavy Boots III', 'arm_tsk_180', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (181, 'Armorsmith Guild Task: 1x Heavy Crest III', 'arm_tsk_181', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (182, 'Armorsmith Guild Task: 1x Large Shield III', 'arm_tsk_182', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (183, 'Armorsmith Guild Task: 1x Leather Tunic III', 'arm_tsk_183', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (184, 'Armorsmith Guild Task: 1x Light Armor Repair Kit III', 'arm_tsk_184', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (185, 'Armorsmith Guild Task: 1x Light Belt III', 'arm_tsk_185', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (186, 'Armorsmith Guild Task: 1x Light Boots III', 'arm_tsk_186', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (187, 'Armorsmith Guild Task: 1x Light Choker III', 'arm_tsk_187', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (188, 'Armorsmith Guild Task: 1x Light Helmet III', 'arm_tsk_188', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (189, 'Armorsmith Guild Task: 1x Power Glove III', 'arm_tsk_189', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (190, 'Armorsmith Guild Task: 1x Shield Repair Kit III', 'arm_tsk_190', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (191, 'Armorsmith Guild Task: 1x Small Shield III', 'arm_tsk_191', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (192, 'Armorsmith Guild Task: 1x Tower Shield III', 'arm_tsk_192', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (193, 'Armorsmith Guild Task: 1x Additional Fuel Tank (Medium)', 'arm_tsk_193', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 84);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (194, 'Armorsmith Guild Task: 1x Additional Stronidium Tank (Medium)', 'arm_tsk_194', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 84);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (195, 'Armorsmith Guild Task: 1x Breastplate IV', 'arm_tsk_195', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (196, 'Armorsmith Guild Task: 1x Force Armor Repair Kit IV', 'arm_tsk_196', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (197, 'Armorsmith Guild Task: 1x Force Boots IV', 'arm_tsk_197', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (198, 'Armorsmith Guild Task: 1x Force Helmet IV', 'arm_tsk_198', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (199, 'Armorsmith Guild Task: 1x Force Necklace IV', 'arm_tsk_199', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (200, 'Armorsmith Guild Task: 1x Force Robes IV', 'arm_tsk_200', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (201, 'Armorsmith Guild Task: 1x Heavy Armor Repair Kit IV', 'arm_tsk_201', 1, 0, 0, 455, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 97);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (202, 'Armorsmith Guild Task: 1x Heavy Boots IV', 'arm_tsk_202', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (203, 'Armorsmith Guild Task: 1x Heavy Crest IV', 'arm_tsk_203', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (204, 'Armorsmith Guild Task: 1x Heavy Helmet IV', 'arm_tsk_204', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (205, 'Armorsmith Guild Task: 1x Hull Plating', 'arm_tsk_205', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (206, 'Armorsmith Guild Task: 1x Large Shield IV', 'arm_tsk_206', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (207, 'Armorsmith Guild Task: 1x Leather Tunic IV', 'arm_tsk_207', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (208, 'Armorsmith Guild Task: 1x Light Armor Repair Kit IV', 'arm_tsk_208', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (209, 'Armorsmith Guild Task: 1x Light Boots IV', 'arm_tsk_209', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (210, 'Armorsmith Guild Task: 1x Light Choker IV', 'arm_tsk_210', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (211, 'Armorsmith Guild Task: 1x Light Helmet IV', 'arm_tsk_211', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (212, 'Armorsmith Guild Task: 1x Power Glove IV', 'arm_tsk_212', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (213, 'Armorsmith Guild Task: 1x Prism Force Necklace', 'arm_tsk_213', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (214, 'Armorsmith Guild Task: 1x Prism Heavy Necklace', 'arm_tsk_214', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (215, 'Armorsmith Guild Task: 1x Prism Light Necklace', 'arm_tsk_215', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (216, 'Armorsmith Guild Task: 1x Prismatic Force Belt', 'arm_tsk_216', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (217, 'Armorsmith Guild Task: 1x Prismatic Heavy Belt', 'arm_tsk_217', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (218, 'Armorsmith Guild Task: 1x Prismatic Light Belt', 'arm_tsk_218', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (219, 'Armorsmith Guild Task: 1x Shield Repair Kit IV', 'arm_tsk_219', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (220, 'Armorsmith Guild Task: 1x Small Shield IV', 'arm_tsk_220', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (221, 'Armorsmith Guild Task: 1x Tower Shield IV', 'arm_tsk_221', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',4, 88);


INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (101, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (101, 'breastplate_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (102, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (102, 'force_boots_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (103, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (103, 'helmet_fb', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (104, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (104, 'force_robe_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (105, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (105, 'heavy_boots_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (106, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (106, 'helmet_hb', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (107, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (107, 'large_shield_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (108, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (108, 'leather_tunic_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (109, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (109, 'light_boots_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (110, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (110, 'helmet_lb', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (111, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (111, 'powerglove_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (112, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (112, 'small_shield_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (113, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (113, 'tower_shield_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (114, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (114, 'padding_fiber', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (115, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (115, 'core_f_armor', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (116, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (116, 'f_armor_segment', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (117, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (117, 'core_h_armor', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (118, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (118, 'h_armor_segment', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (119, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (119, 'core_l_armor', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (120, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (120, 'l_armor_segment', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (121, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (121, 'padding_metal', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (122, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (122, 'breastplate_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (123, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (123, 'fa_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (124, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (124, 'force_belt_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (125, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (125, 'force_boots_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (126, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (126, 'helmet_f1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (127, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (127, 'force_neck_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (128, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (128, 'force_robe_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (129, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (129, 'ha_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (130, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (130, 'heavy_belt_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (131, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (131, 'heavy_boots_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (132, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (132, 'h_crest_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (133, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (133, 'helmet_h1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (134, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (134, 'large_shield_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (135, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (135, 'leather_tunic_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (136, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (136, 'la_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (137, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (137, 'light_belt_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (138, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (138, 'light_boots_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (139, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (139, 'lt_choker_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (140, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (140, 'helmet_l1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (141, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (141, 'powerglove_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (142, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (142, 'sh_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (143, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (143, 'small_shield_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (144, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (144, 'tower_shield_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (145, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (145, 'breastplate_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (146, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (146, 'fa_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (147, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (147, 'force_belt_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (148, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (148, 'force_boots_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (149, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (149, 'helmet_f2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (150, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (150, 'force_neck_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (151, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (151, 'force_robe_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (152, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (152, 'ha_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (153, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (153, 'heavy_belt_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (154, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (154, 'heavy_boots_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (155, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (155, 'h_crest_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (156, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (156, 'helmet_h2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (157, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (157, 'helmet_h3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (158, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (158, 'large_shield_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (159, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (159, 'leather_tunic_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (160, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (160, 'la_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (161, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (161, 'light_belt_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (162, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (162, 'light_boots_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (163, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (163, 'lt_choker_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (164, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (164, 'helmet_l2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (165, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (165, 'powerglove_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (166, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (166, 'sh_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (167, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (167, 'small_shield_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (168, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (168, 'tower_shield_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (169, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (169, 'ssfuel1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (170, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (170, 'ssstron1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (171, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (171, 'breastplate_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (172, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (172, 'fa_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (173, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (173, 'force_belt_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (174, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (174, 'force_boots_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (175, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (175, 'helmet_f3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (176, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (176, 'force_neck_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (177, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (177, 'force_robe_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (178, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (178, 'ha_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (179, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (179, 'heavy_belt_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (180, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (180, 'heavy_boots_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (181, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (181, 'h_crest_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (182, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (182, 'large_shield_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (183, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (183, 'leather_tunic_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (184, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (184, 'la_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (185, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (185, 'light_belt_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (186, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (186, 'light_boots_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (187, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (187, 'lt_choker_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (188, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (188, 'helmet_l3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (189, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (189, 'powerglove_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (190, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (190, 'sh_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (191, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (191, 'small_shield_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (192, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (192, 'tower_shield_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (193, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (193, 'ssfuel2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (194, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (194, 'ssstron2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (195, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (195, 'breastplate_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (196, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (196, 'fa_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (197, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (197, 'force_boots_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (198, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (198, 'helmet_f4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (199, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (199, 'force_neck_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (200, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (200, 'force_robe_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (201, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (201, 'ha_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (202, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (202, 'heavy_boots_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (203, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (203, 'h_crest_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (204, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (204, 'helmet_h4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (205, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (205, 'hull_plating', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (206, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (206, 'large_shield_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (207, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (207, 'leather_tunic_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (208, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (208, 'la_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (209, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (209, 'light_boots_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (210, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (210, 'lt_choker_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (211, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (211, 'helmet_l4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (212, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (212, 'powerglove_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (213, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (213, 'prism_neck_f', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (214, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (214, 'prism_neck_h', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (215, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (215, 'prism_neck_l', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (216, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (216, 'prism_belt_f', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (217, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (217, 'prism_belt_h', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (218, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (218, 'prism_belt_l', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (219, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (219, 'sh_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (220, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (220, 'small_shield_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (221, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (221, 'tower_shield_4', 1, SCOPE_IDENTITY(), 1);


INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 101, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 102, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 103, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 104, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 105, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 106, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 107, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 108, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 109, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 110, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 111, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 112, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 113, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 114, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 115, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 116, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 117, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 118, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 119, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 120, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 121, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 122, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 123, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 124, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 125, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 126, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 127, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 128, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 129, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 130, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 131, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 132, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 133, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 134, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 135, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 136, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 137, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 138, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 139, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 140, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 141, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 142, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 143, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 144, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 145, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 146, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 147, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 148, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 149, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 150, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 151, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 152, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 153, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 154, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 155, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 156, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 157, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 158, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 159, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 160, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 161, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 162, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 163, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 164, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 165, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 166, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 167, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 168, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 169, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 170, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 171, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 172, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 173, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 174, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 175, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 176, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 177, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 178, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 179, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 180, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 181, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 182, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 183, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 184, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 185, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 186, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 187, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 188, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 189, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 190, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 191, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 192, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 193, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 194, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 195, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 196, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 197, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 198, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 199, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 200, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 201, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 202, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 203, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 204, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 205, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 206, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 207, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 208, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 209, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 210, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 211, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 212, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 213, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 214, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 215, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 216, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 217, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 218, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 219, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 220, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (4, 221, 4, 0);


-- ARMORSMITH INSERTS END


-- WEAPONSMITH INSERTS BEGIN

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (222, 'Weaponsmith Guild Task: 1x Basic Baton C', 'wpn_tsk_222', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (223, 'Weaponsmith Guild Task: 1x Basic Baton M', 'wpn_tsk_223', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (224, 'Weaponsmith Guild Task: 1x Basic Baton MS', 'wpn_tsk_224', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (225, 'Weaponsmith Guild Task: 1x Basic Finesse Vibroblade D', 'wpn_tsk_225', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (226, 'Weaponsmith Guild Task: 1x Basic Finesse Vibroblade K', 'wpn_tsk_226', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (227, 'Weaponsmith Guild Task: 1x Basic Finesse Vibroblade R', 'wpn_tsk_227', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (228, 'Weaponsmith Guild Task: 1x Basic Finesse Vibroblade SS', 'wpn_tsk_228', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (229, 'Weaponsmith Guild Task: 1x Basic Heavy Vibroblade GA', 'wpn_tsk_229', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (230, 'Weaponsmith Guild Task: 1x Basic Heavy Vibroblade GS', 'wpn_tsk_230', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (231, 'Weaponsmith Guild Task: 1x Basic Polearm H', 'wpn_tsk_231', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (232, 'Weaponsmith Guild Task: 1x Basic Polearm S', 'wpn_tsk_232', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (233, 'Weaponsmith Guild Task: 1x Basic Quarterstaff', 'wpn_tsk_233', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (234, 'Weaponsmith Guild Task: 1x Basic Twin Vibroblade DA', 'wpn_tsk_234', 1, 0, 0, 55, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 14);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (235, 'Weaponsmith Guild Task: 1x Basic Twin Vibroblade TS', 'wpn_tsk_235', 1, 0, 0, 55, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 14);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (236, 'Weaponsmith Guild Task: 1x Basic Vibroblade BA', 'wpn_tsk_236', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (237, 'Weaponsmith Guild Task: 1x Basic Vibroblade BS', 'wpn_tsk_237', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (238, 'Weaponsmith Guild Task: 1x Basic Vibroblade K', 'wpn_tsk_238', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (239, 'Weaponsmith Guild Task: 1x Basic Vibroblade LS', 'wpn_tsk_239', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (240, 'Weaponsmith Guild Task: 1x Large Blade', 'wpn_tsk_240', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (241, 'Weaponsmith Guild Task: 1x Large Handle', 'wpn_tsk_241', 1, 0, 0, 80, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 20);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (242, 'Weaponsmith Guild Task: 1x Medium Blade', 'wpn_tsk_242', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (243, 'Weaponsmith Guild Task: 1x Medium Handle', 'wpn_tsk_243', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (244, 'Weaponsmith Guild Task: 1x Metal Baton Frame', 'wpn_tsk_244', 1, 0, 0, 40, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 10);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (245, 'Weaponsmith Guild Task: 1x Shaft', 'wpn_tsk_245', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (246, 'Weaponsmith Guild Task: 1x Small Blade', 'wpn_tsk_246', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (247, 'Weaponsmith Guild Task: 1x Small Handle', 'wpn_tsk_247', 1, 0, 0, 40, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 10);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (248, 'Weaponsmith Guild Task: 1x Wood Baton Frame', 'wpn_tsk_248', 1, 0, 0, 40, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 10);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (249, 'Weaponsmith Guild Task: 1x Baton C1', 'wpn_tsk_249', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (250, 'Weaponsmith Guild Task: 1x Baton M1', 'wpn_tsk_250', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (251, 'Weaponsmith Guild Task: 1x Baton MS1', 'wpn_tsk_251', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (252, 'Weaponsmith Guild Task: 1x Baton Repair Kit I', 'wpn_tsk_252', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (253, 'Weaponsmith Guild Task: 1x Finesse Vibroblade D1', 'wpn_tsk_253', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (254, 'Weaponsmith Guild Task: 1x Finesse Vibroblade K1', 'wpn_tsk_254', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (255, 'Weaponsmith Guild Task: 1x Finesse Vibroblade R1', 'wpn_tsk_255', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (256, 'Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit I', 'wpn_tsk_256', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (257, 'Weaponsmith Guild Task: 1x Finesse Vibroblade SS1', 'wpn_tsk_257', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (258, 'Weaponsmith Guild Task: 1x Heavy Vibroblade GA1', 'wpn_tsk_258', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (259, 'Weaponsmith Guild Task: 1x Heavy Vibroblade GS1', 'wpn_tsk_259', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (260, 'Weaponsmith Guild Task: 1x Heavy Vibroblade Repair Kit I', 'wpn_tsk_260', 1, 0, 0, 155, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 37);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (261, 'Weaponsmith Guild Task: 1x Martial Arts Weapon Repair Kit I', 'wpn_tsk_261', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (262, 'Weaponsmith Guild Task: 1x Polearm H1', 'wpn_tsk_262', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (263, 'Weaponsmith Guild Task: 1x Polearm Repair Kit I', 'wpn_tsk_263', 1, 0, 0, 155, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 37);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (264, 'Weaponsmith Guild Task: 1x Polearm S1', 'wpn_tsk_264', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (265, 'Weaponsmith Guild Task: 1x Quarterstaff I', 'wpn_tsk_265', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (266, 'Weaponsmith Guild Task: 1x Twin Vibroblade DA1', 'wpn_tsk_266', 1, 0, 0, 105, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 24);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (267, 'Weaponsmith Guild Task: 1x Twin Vibroblade Repair Kit I', 'wpn_tsk_267', 1, 0, 0, 155, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 37);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (268, 'Weaponsmith Guild Task: 1x Twin Vibroblade TS1', 'wpn_tsk_268', 1, 0, 0, 105, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 24);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (269, 'Weaponsmith Guild Task: 1x Vibroblade BA1', 'wpn_tsk_269', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (270, 'Weaponsmith Guild Task: 1x Vibroblade BS1', 'wpn_tsk_270', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (271, 'Weaponsmith Guild Task: 1x Vibroblade K1', 'wpn_tsk_271', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (272, 'Weaponsmith Guild Task: 1x Vibroblade LS1', 'wpn_tsk_272', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (273, 'Weaponsmith Guild Task: 1x Vibroblade Repair Kit I', 'wpn_tsk_273', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (274, 'Weaponsmith Guild Task: 1x Baton C2', 'wpn_tsk_274', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (275, 'Weaponsmith Guild Task: 1x Baton M2', 'wpn_tsk_275', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (276, 'Weaponsmith Guild Task: 1x Baton MS2', 'wpn_tsk_276', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (277, 'Weaponsmith Guild Task: 1x Baton Repair Kit II', 'wpn_tsk_277', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (278, 'Weaponsmith Guild Task: 1x Finesse Vibroblade D2', 'wpn_tsk_278', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (279, 'Weaponsmith Guild Task: 1x Finesse Vibroblade K2', 'wpn_tsk_279', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (280, 'Weaponsmith Guild Task: 1x Finesse Vibroblade R2', 'wpn_tsk_280', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (281, 'Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit II', 'wpn_tsk_281', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (282, 'Weaponsmith Guild Task: 1x Finesse Vibroblade SS2', 'wpn_tsk_282', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (283, 'Weaponsmith Guild Task: 1x Heavy Vibroblade GA2', 'wpn_tsk_283', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (284, 'Weaponsmith Guild Task: 1x Heavy Vibroblade GS2', 'wpn_tsk_284', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (285, 'Weaponsmith Guild Task: 1x Heavy Vibroblade Repair Kit II', 'wpn_tsk_285', 1, 0, 0, 255, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 57);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (286, 'Weaponsmith Guild Task: 1x Martial Arts Weapon Repair Kit II', 'wpn_tsk_286', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (287, 'Weaponsmith Guild Task: 1x Polearm H2', 'wpn_tsk_287', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (288, 'Weaponsmith Guild Task: 1x Polearm Repair Kit II', 'wpn_tsk_288', 1, 0, 0, 255, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 57);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (289, 'Weaponsmith Guild Task: 1x Polearm S2', 'wpn_tsk_289', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (290, 'Weaponsmith Guild Task: 1x Quarterstaff II', 'wpn_tsk_290', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (291, 'Weaponsmith Guild Task: 1x Twin Vibroblade DA2', 'wpn_tsk_291', 1, 0, 0, 205, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 44);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (292, 'Weaponsmith Guild Task: 1x Twin Vibroblade Repair Kit II', 'wpn_tsk_292', 1, 0, 0, 255, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 57);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (293, 'Weaponsmith Guild Task: 1x Twin Vibroblade TS2', 'wpn_tsk_293', 1, 0, 0, 205, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 44);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (294, 'Weaponsmith Guild Task: 1x Vibroblade BA2', 'wpn_tsk_294', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (295, 'Weaponsmith Guild Task: 1x Vibroblade BS2', 'wpn_tsk_295', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (296, 'Weaponsmith Guild Task: 1x Vibroblade K2', 'wpn_tsk_296', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (297, 'Weaponsmith Guild Task: 1x Vibroblade LS2', 'wpn_tsk_297', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (298, 'Weaponsmith Guild Task: 1x Vibroblade Repair Kit II', 'wpn_tsk_298', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (299, 'Weaponsmith Guild Task: 1x Baton C3', 'wpn_tsk_299', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (300, 'Weaponsmith Guild Task: 1x Baton M3', 'wpn_tsk_300', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (301, 'Weaponsmith Guild Task: 1x Baton MS3', 'wpn_tsk_301', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (302, 'Weaponsmith Guild Task: 1x Baton Repair Kit III', 'wpn_tsk_302', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (303, 'Weaponsmith Guild Task: 1x Finesse Vibroblade D3', 'wpn_tsk_303', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (304, 'Weaponsmith Guild Task: 1x Finesse Vibroblade K3', 'wpn_tsk_304', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (305, 'Weaponsmith Guild Task: 1x Finesse Vibroblade R3', 'wpn_tsk_305', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (306, 'Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit III', 'wpn_tsk_306', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (307, 'Weaponsmith Guild Task: 1x Finesse Vibroblade SS3', 'wpn_tsk_307', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (308, 'Weaponsmith Guild Task: 1x Heavy Vibroblade GA3', 'wpn_tsk_308', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (309, 'Weaponsmith Guild Task: 1x Heavy Vibroblade GS3', 'wpn_tsk_309', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (310, 'Weaponsmith Guild Task: 1x Heavy Vibroblade Repair Kit III', 'wpn_tsk_310', 1, 0, 0, 355, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 77);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (311, 'Weaponsmith Guild Task: 1x Martial Arts Weapon Repair Kit III', 'wpn_tsk_311', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (312, 'Weaponsmith Guild Task: 1x Polearm H3', 'wpn_tsk_312', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (313, 'Weaponsmith Guild Task: 1x Polearm Repair Kit III', 'wpn_tsk_313', 1, 0, 0, 355, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 77);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (314, 'Weaponsmith Guild Task: 1x Polearm S3', 'wpn_tsk_314', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (315, 'Weaponsmith Guild Task: 1x Quarterstaff III', 'wpn_tsk_315', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (316, 'Weaponsmith Guild Task: 1x Twin Vibroblade DA3', 'wpn_tsk_316', 1, 0, 0, 305, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 64);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (317, 'Weaponsmith Guild Task: 1x Twin Vibroblade Repair Kit III', 'wpn_tsk_317', 1, 0, 0, 355, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 77);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (318, 'Weaponsmith Guild Task: 1x Twin Vibroblade TS3', 'wpn_tsk_318', 1, 0, 0, 305, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 64);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (319, 'Weaponsmith Guild Task: 1x Vibroblade BA3', 'wpn_tsk_319', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (320, 'Weaponsmith Guild Task: 1x Vibroblade BS3', 'wpn_tsk_320', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (321, 'Weaponsmith Guild Task: 1x Vibroblade K3', 'wpn_tsk_321', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (322, 'Weaponsmith Guild Task: 1x Vibroblade LS3', 'wpn_tsk_322', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (323, 'Weaponsmith Guild Task: 1x Vibroblade Repair Kit III', 'wpn_tsk_323', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (324, 'Weaponsmith Guild Task: 1x Baton C4', 'wpn_tsk_324', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (325, 'Weaponsmith Guild Task: 1x Baton M4', 'wpn_tsk_325', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (326, 'Weaponsmith Guild Task: 1x Baton MS4', 'wpn_tsk_326', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (327, 'Weaponsmith Guild Task: 1x Baton Repair Kit IV', 'wpn_tsk_327', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (328, 'Weaponsmith Guild Task: 1x Finesse Vibroblade D4', 'wpn_tsk_328', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (329, 'Weaponsmith Guild Task: 1x Finesse Vibroblade K4', 'wpn_tsk_329', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (330, 'Weaponsmith Guild Task: 1x Finesse Vibroblade R4', 'wpn_tsk_330', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (331, 'Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit IV', 'wpn_tsk_331', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (332, 'Weaponsmith Guild Task: 1x Finesse Vibroblade SS4', 'wpn_tsk_332', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (333, 'Weaponsmith Guild Task: 1x Heavy Vibroblade GA4', 'wpn_tsk_333', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (334, 'Weaponsmith Guild Task: 1x Heavy Vibroblade GS4', 'wpn_tsk_334', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (335, 'Weaponsmith Guild Task: 1x Heavy Vibroblade Repair Kit IV', 'wpn_tsk_335', 1, 0, 0, 455, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 97);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (336, 'Weaponsmith Guild Task: 1x Martial Arts Weapon Repair Kit IV', 'wpn_tsk_336', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (337, 'Weaponsmith Guild Task: 1x Polearm H4', 'wpn_tsk_337', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (338, 'Weaponsmith Guild Task: 1x Polearm Repair Kit IV', 'wpn_tsk_338', 1, 0, 0, 455, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 97);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (339, 'Weaponsmith Guild Task: 1x Polearm S4', 'wpn_tsk_339', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (340, 'Weaponsmith Guild Task: 1x Quarterstaff IV', 'wpn_tsk_340', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (341, 'Weaponsmith Guild Task: 1x Twin Vibroblade DA4', 'wpn_tsk_341', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 84);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (342, 'Weaponsmith Guild Task: 1x Twin Vibroblade Repair Kit IV', 'wpn_tsk_342', 1, 0, 0, 455, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 97);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (343, 'Weaponsmith Guild Task: 1x Twin Vibroblade TS4', 'wpn_tsk_343', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 84);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (344, 'Weaponsmith Guild Task: 1x Vibroblade BA4', 'wpn_tsk_344', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (345, 'Weaponsmith Guild Task: 1x Vibroblade BS4', 'wpn_tsk_345', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (346, 'Weaponsmith Guild Task: 1x Vibroblade K4', 'wpn_tsk_346', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (347, 'Weaponsmith Guild Task: 1x Vibroblade LS4', 'wpn_tsk_347', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (348, 'Weaponsmith Guild Task: 1x Vibroblade Repair Kit IV', 'wpn_tsk_348', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',3, 88);

INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (222, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (222, 'club_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (223, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (223, 'mace_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (224, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (224, 'morningstar_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (225, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (225, 'dagger_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (226, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (226, 'kukri_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (227, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (227, 'rapier_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (228, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (228, 'shortsword_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (229, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (229, 'greataxe_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (230, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (230, 'greatsword_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (231, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (231, 'halberd_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (232, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (232, 'spear_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (233, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (233, 'quarterstaff_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (234, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (234, 'doubleaxe_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (235, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (235, 'twinblade_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (236, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (236, 'battleaxe_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (237, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (237, 'bst_sword_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (238, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (238, 'katana_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (239, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (239, 'longsword_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (240, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (240, 'large_blade', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (241, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (241, 'large_handle', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (242, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (242, 'medium_blade', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (243, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (243, 'medium_handle', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (244, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (244, 'm_baton_frame', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (245, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (245, 'shaft', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (246, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (246, 'small_blade', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (247, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (247, 'small_handle', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (248, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (248, 'w_baton_frame', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (249, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (249, 'club_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (250, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (250, 'mace_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (251, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (251, 'morningstar_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (252, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (252, 'bt_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (253, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (253, 'dagger_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (254, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (254, 'kukri_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (255, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (255, 'rapier_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (256, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (256, 'fv_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (257, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (257, 'shortsword_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (258, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (258, 'greataxe_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (259, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (259, 'greatsword_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (260, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (260, 'hv_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (261, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (261, 'ma_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (262, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (262, 'halberd_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (263, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (263, 'po_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (264, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (264, 'spear_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (265, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (265, 'quarterstaff_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (266, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (266, 'doubleaxe_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (267, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (267, 'tb_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (268, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (268, 'twinblade_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (269, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (269, 'battleaxe_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (270, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (270, 'bst_sword_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (271, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (271, 'katana_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (272, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (272, 'longsword_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (273, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (273, 'vb_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (274, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (274, 'club_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (275, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (275, 'mace_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (276, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (276, 'morningstar_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (277, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (277, 'bt_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (278, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (278, 'dagger_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (279, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (279, 'kukri_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (280, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (280, 'rapier_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (281, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (281, 'fv_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (282, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (282, 'shortsword_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (283, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (283, 'greataxe_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (284, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (284, 'greatsword_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (285, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (285, 'hv_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (286, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (286, 'ma_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (287, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (287, 'halberd_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (288, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (288, 'po_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (289, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (289, 'spear_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (290, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (290, 'quarterstaff_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (291, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (291, 'doubleaxe_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (292, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (292, 'tb_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (293, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (293, 'twinblade_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (294, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (294, 'battleaxe_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (295, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (295, 'bst_sword_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (296, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (296, 'katana_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (297, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (297, 'longsword_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (298, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (298, 'vb_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (299, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (299, 'club_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (300, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (300, 'mace_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (301, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (301, 'morningstar_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (302, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (302, 'bt_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (303, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (303, 'dagger_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (304, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (304, 'kukri_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (305, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (305, 'rapier_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (306, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (306, 'fv_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (307, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (307, 'shortsword_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (308, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (308, 'greataxe_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (309, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (309, 'greatsword_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (310, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (310, 'hv_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (311, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (311, 'ma_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (312, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (312, 'halberd_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (313, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (313, 'po_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (314, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (314, 'spear_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (315, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (315, 'quarterstaff_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (316, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (316, 'doubleaxe_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (317, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (317, 'tb_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (318, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (318, 'twinblade_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (319, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (319, 'battleaxe_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (320, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (320, 'bst_sword_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (321, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (321, 'katana_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (322, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (322, 'longsword_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (323, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (323, 'vb_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (324, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (324, 'club_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (325, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (325, 'mace_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (326, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (326, 'morningstar_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (327, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (327, 'bt_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (328, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (328, 'dagger_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (329, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (329, 'kukri_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (330, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (330, 'rapier_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (331, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (331, 'fv_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (332, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (332, 'shortsword_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (333, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (333, 'greataxe_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (334, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (334, 'greatsword_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (335, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (335, 'hv_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (336, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (336, 'ma_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (337, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (337, 'halberd_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (338, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (338, 'po_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (339, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (339, 'spear_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (340, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (340, 'quarterstaff_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (341, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (341, 'doubleaxe_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (342, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (342, 'tb_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (343, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (343, 'twinblade_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (344, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (344, 'battleaxe_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (345, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (345, 'bst_sword_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (346, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (346, 'katana_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (347, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (347, 'longsword_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (348, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (348, 'vb_rep_4', 1, SCOPE_IDENTITY(), 1);

INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 222, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 223, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 224, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 225, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 226, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 227, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 228, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 229, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 230, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 231, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 232, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 233, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 234, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 235, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 236, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 237, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 238, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 239, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 240, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 241, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 242, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 243, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 244, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 245, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 246, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 247, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 248, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 249, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 250, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 251, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 252, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 253, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 254, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 255, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 256, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 257, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 258, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 259, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 260, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 261, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 262, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 263, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 264, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 265, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 266, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 267, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 268, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 269, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 270, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 271, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 272, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 273, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 274, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 275, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 276, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 277, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 278, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 279, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 280, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 281, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 282, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 283, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 284, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 285, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 286, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 287, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 288, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 289, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 290, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 291, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 292, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 293, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 294, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 295, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 296, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 297, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 298, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 299, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 300, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 301, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 302, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 303, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 304, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 305, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 306, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 307, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 308, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 309, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 310, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 311, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 312, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 313, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 314, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 315, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 316, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 317, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 318, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 319, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 320, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 321, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 322, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 323, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 324, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 325, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 326, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 327, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 328, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 329, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 330, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 331, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 332, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 333, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 334, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 335, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 336, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 337, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 338, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 339, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 340, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 341, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 342, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 343, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 344, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 345, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 346, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 347, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (3, 348, 4, 0);


-- WEAPONSMITH INSERTS END


-- ENGINEERING INSERTS START

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (349, 'Engineering Guild Task: 1x Basic Blaster Pistol', 'eng_tsk_349', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (350, 'Engineering Guild Task: 1x Basic Blaster Rifle', 'eng_tsk_350', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (351, 'Engineering Guild Task: 1x Basic Mineral Scanner', 'eng_tsk_351', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (352, 'Engineering Guild Task: 1x Basic Resource Harvester', 'eng_tsk_352', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (353, 'Engineering Guild Task: 1x Basic Resource Scanner', 'eng_tsk_353', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (354, 'Engineering Guild Task: 1x Blue Crystal Cluster', 'eng_tsk_354', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 30);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (355, 'Engineering Guild Task: 1x Fuel Cell', 'eng_tsk_355', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (356, 'Engineering Guild Task: 1x Green Crystal Cluster', 'eng_tsk_356', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 30);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (357, 'Engineering Guild Task: 1x Pistol Barrel', 'eng_tsk_357', 1, 0, 0, 40, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 10);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (358, 'Engineering Guild Task: 1x Power Crystal Cluster', 'eng_tsk_358', 1, 0, 0, 40, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 10);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (359, 'Engineering Guild Task: 1x Ranged Weapon Core', 'eng_tsk_359', 1, 0, 0, 55, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 14);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (360, 'Engineering Guild Task: 1x Red Crystal Cluster', 'eng_tsk_360', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 30);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (361, 'Engineering Guild Task: 1x Rifle Barrel', 'eng_tsk_361', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (362, 'Engineering Guild Task: 1x Yellow Crystal Cluster', 'eng_tsk_362', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 30);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (363, 'Engineering Guild Task: 1x Activation Speed I', 'eng_tsk_363', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (364, 'Engineering Guild Task: 1x Armor Class I', 'eng_tsk_364', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (365, 'Engineering Guild Task: 1x Armorsmith I', 'eng_tsk_365', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (366, 'Engineering Guild Task: 1x Attack Bonus I', 'eng_tsk_366', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (367, 'Engineering Guild Task: 1x Basic Training Foil (Blue)', 'eng_tsk_367', 1, 0, 0, 105, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 25);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (368, 'Engineering Guild Task: 1x Basic Training Foil (Green)', 'eng_tsk_368', 1, 0, 0, 105, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 25);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (369, 'Engineering Guild Task: 1x Basic Training Foil (Red)', 'eng_tsk_369', 1, 0, 0, 105, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 25);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (370, 'Engineering Guild Task: 1x Basic Training Foil (Yellow)', 'eng_tsk_370', 1, 0, 0, 105, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 25);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (371, 'Engineering Guild Task: 1x Basic Training  (Blue)', 'eng_tsk_371', 1, 0, 0, 125, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 30);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (372, 'Engineering Guild Task: 1x Basic Training Foil Staff (Green)', 'eng_tsk_372', 1, 0, 0, 125, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 30);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (373, 'Engineering Guild Task: 1x Basic Training Foil Staff (Red)', 'eng_tsk_373', 1, 0, 0, 125, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 30);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (374, 'Engineering Guild Task: 1x Basic Training Foil Staff (Yellow)', 'eng_tsk_374', 1, 0, 0, 125, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 30);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (375, 'Engineering Guild Task: 1x Blaster Pistol I', 'eng_tsk_375', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (376, 'Engineering Guild Task: 1x Blaster Pistol Repair Kit I', 'eng_tsk_376', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (377, 'Engineering Guild Task: 1x Blaster Rifle I', 'eng_tsk_377', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (378, 'Engineering Guild Task: 1x Blaster Rifle Repair Kit I', 'eng_tsk_378', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (379, 'Engineering Guild Task: 1x Charisma I', 'eng_tsk_379', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (380, 'Engineering Guild Task: 1x Constitution I', 'eng_tsk_380', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (381, 'Engineering Guild Task: 1x Cooking I', 'eng_tsk_381', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (382, 'Engineering Guild Task: 1x Damage I', 'eng_tsk_382', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (383, 'Engineering Guild Task: 1x Dark Defense I', 'eng_tsk_383', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (384, 'Engineering Guild Task: 1x Dark Potency I', 'eng_tsk_384', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (385, 'Engineering Guild Task: 1x Dexterity I', 'eng_tsk_385', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (386, 'Engineering Guild Task: 1x Electrical Defense I', 'eng_tsk_386', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (387, 'Engineering Guild Task: 1x Electrical Potency I', 'eng_tsk_387', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (388, 'Engineering Guild Task: 1x Emitter', 'eng_tsk_388', 1, 0, 0, 90, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 20);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (389, 'Engineering Guild Task: 1x Engineering I', 'eng_tsk_389', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (390, 'Engineering Guild Task: 1x Fabrication I', 'eng_tsk_390', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (391, 'Engineering Guild Task: 1x First Aid I', 'eng_tsk_391', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (392, 'Engineering Guild Task: 1x FP I', 'eng_tsk_392', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (393, 'Engineering Guild Task: 1x Fuel Cell', 'eng_tsk_393', 1, 0, 0, 90, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 20);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (394, 'Engineering Guild Task: 1x Harvesting I', 'eng_tsk_394', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (395, 'Engineering Guild Task: 1x Hit Points I', 'eng_tsk_395', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (396, 'Engineering Guild Task: 1x Improved Enmity', 'eng_tsk_396', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (397, 'Engineering Guild Task: 1x Intelligence I', 'eng_tsk_397', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (398, 'Engineering Guild Task: 1x Light Defense I', 'eng_tsk_398', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (399, 'Engineering Guild Task: 1x Light Potency I', 'eng_tsk_399', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (400, 'Engineering Guild Task: 1x Lightsaber Repair Kit I', 'eng_tsk_400', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (401, 'Engineering Guild Task: 1x Mind Defense I', 'eng_tsk_401', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (402, 'Engineering Guild Task: 1x Mind Potency I', 'eng_tsk_402', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (403, 'Engineering Guild Task: 1x Resource Harvester I', 'eng_tsk_403', 1, 0, 0, 110, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 25);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (404, 'Engineering Guild Task: 1x Resource Scanner I', 'eng_tsk_404', 1, 0, 0, 110, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 25);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (405, 'Engineering Guild Task: 1x Saber Hilt', 'eng_tsk_405', 1, 0, 0, 110, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 25);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (406, 'Engineering Guild Task: 1x Saberstaff Repair Kit I', 'eng_tsk_406', 1, 0, 0, 155, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 37);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (407, 'Engineering Guild Task: 1x Sneak Attack I', 'eng_tsk_407', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (408, 'Engineering Guild Task: 1x Strength I', 'eng_tsk_408', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (409, 'Engineering Guild Task: 1x Weaponsmith I', 'eng_tsk_409', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (410, 'Engineering Guild Task: 1x Wisdom I', 'eng_tsk_410', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (411, 'Engineering Guild Task: 1x Activation Speed II', 'eng_tsk_411', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (412, 'Engineering Guild Task: 1x Attack Bonus II', 'eng_tsk_412', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (413, 'Engineering Guild Task: 1x Base Attack Bonus I', 'eng_tsk_413', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (414, 'Engineering Guild Task: 1x Blaster Pistol II', 'eng_tsk_414', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (415, 'Engineering Guild Task: 1x Blaster Pistol Repair Kit II', 'eng_tsk_415', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (416, 'Engineering Guild Task: 1x Blaster Rifle II', 'eng_tsk_416', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (417, 'Engineering Guild Task: 1x Blaster Rifle Repair Kit II', 'eng_tsk_417', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (418, 'Engineering Guild Task: 1x Charisma II', 'eng_tsk_418', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (419, 'Engineering Guild Task: 1x Constitution II', 'eng_tsk_419', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (420, 'Engineering Guild Task: 1x Dexterity II', 'eng_tsk_420', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (421, 'Engineering Guild Task: 1x Durability I', 'eng_tsk_421', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (422, 'Engineering Guild Task: 1x Enhancement Bonus I', 'eng_tsk_422', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (423, 'Engineering Guild Task: 1x FP Regen', 'eng_tsk_423', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (424, 'Engineering Guild Task: 1x Fuel Cell', 'eng_tsk_424', 1, 0, 0, 210, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 45);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (425, 'Engineering Guild Task: 1x HP Regen', 'eng_tsk_425', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (426, 'Engineering Guild Task: 1x Intelligence II', 'eng_tsk_426', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (427, 'Engineering Guild Task: 1x Level Decrease I', 'eng_tsk_427', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (428, 'Engineering Guild Task: 1x Level Increase I', 'eng_tsk_428', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (429, 'Engineering Guild Task: 1x Training Foil I (Blue)', 'eng_tsk_429', 1, 0, 0, 205, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 45);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (430, 'Engineering Guild Task: 1x Training Foil I (Green)', 'eng_tsk_430', 1, 0, 0, 205, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 45);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (431, 'Engineering Guild Task: 1x Training Foil I (Red)', 'eng_tsk_431', 1, 0, 0, 205, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 45);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (432, 'Engineering Guild Task: 1x Training Foil I (Yellow)', 'eng_tsk_432', 1, 0, 0, 205, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 45);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (433, 'Engineering Guild Task: 1x Lightsaber Repair Kit II', 'eng_tsk_433', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (434, 'Engineering Guild Task: 1x Luck I', 'eng_tsk_434', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (435, 'Engineering Guild Task: 1x Luck II', 'eng_tsk_435', 1, 0, 0, 230, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 50);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (436, 'Engineering Guild Task: 1x Meditate I', 'eng_tsk_436', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (437, 'Engineering Guild Task: 1x Meditate II', 'eng_tsk_437', 1, 0, 0, 230, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 50);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (438, 'Engineering Guild Task: 1x Reduced Enmity I', 'eng_tsk_438', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (439, 'Engineering Guild Task: 1x Resource Harvester II', 'eng_tsk_439', 1, 0, 0, 210, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 45);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (440, 'Engineering Guild Task: 1x Resource Scanner II', 'eng_tsk_440', 1, 0, 0, 210, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 45);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (441, 'Engineering Guild Task: 1x Training Foil Staff I (Blue)', 'eng_tsk_441', 1, 0, 0, 225, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 50);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (442, 'Engineering Guild Task: 1x Training Foil Staff I (Green)', 'eng_tsk_442', 1, 0, 0, 225, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 50);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (443, 'Engineering Guild Task: 1x Training Foil Staff I (Red)', 'eng_tsk_443', 1, 0, 0, 225, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 50);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (444, 'Engineering Guild Task: 1x Training Foil Staff I (Yellow)', 'eng_tsk_444', 1, 0, 0, 225, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 50);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (445, 'Engineering Guild Task: 1x Saberstaff Repair Kit II', 'eng_tsk_445', 1, 0, 0, 255, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 57);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (446, 'Engineering Guild Task: 1x Strength II', 'eng_tsk_446', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (447, 'Engineering Guild Task: 1x Wisdom II', 'eng_tsk_447', 1, 0, 0, 190, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 40);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (448, 'Engineering Guild Task: 1x Armor Class II', 'eng_tsk_448', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (449, 'Engineering Guild Task: 1x Armorsmith II', 'eng_tsk_449', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (450, 'Engineering Guild Task: 1x Auxiliary Shield Generator (Small)', 'eng_tsk_450', 1, 0, 0, 360, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 78);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (451, 'Engineering Guild Task: 1x Auxiliary Targeter (Basic)', 'eng_tsk_451', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (452, 'Engineering Guild Task: 1x Auxiliary Thruster (Small)', 'eng_tsk_452', 1, 0, 0, 340, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 73);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (453, 'Engineering Guild Task: 1x Base Attack Bonus II', 'eng_tsk_453', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (454, 'Engineering Guild Task: 1x Blaster Pistol III', 'eng_tsk_454', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (455, 'Engineering Guild Task: 1x Blaster Pistol Repair Kit III', 'eng_tsk_455', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (456, 'Engineering Guild Task: 1x Blaster Rifle III', 'eng_tsk_456', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (457, 'Engineering Guild Task: 1x Blaster Rifle Repair Kit III', 'eng_tsk_457', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (458, 'Engineering Guild Task: 1x Cloaking Generator (Small)', 'eng_tsk_458', 1, 0, 0, 340, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 73);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (459, 'Engineering Guild Task: 1x Cooking II', 'eng_tsk_459', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (460, 'Engineering Guild Task: 1x Damage II', 'eng_tsk_460', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (461, 'Engineering Guild Task: 1x Dark Defense II', 'eng_tsk_461', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (462, 'Engineering Guild Task: 1x Dark Potency II', 'eng_tsk_462', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (463, 'Engineering Guild Task: 1x Durability II', 'eng_tsk_463', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (464, 'Engineering Guild Task: 1x Electrical Defense II', 'eng_tsk_464', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (465, 'Engineering Guild Task: 1x Electrical Potency II', 'eng_tsk_465', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (466, 'Engineering Guild Task: 1x Engineering II', 'eng_tsk_466', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (467, 'Engineering Guild Task: 1x Enhancement Bonus II', 'eng_tsk_467', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (468, 'Engineering Guild Task: 1x Fabrication II', 'eng_tsk_468', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (469, 'Engineering Guild Task: 1x First Aid II', 'eng_tsk_469', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (470, 'Engineering Guild Task: 1x FP II', 'eng_tsk_470', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (471, 'Engineering Guild Task: 1x FP Regen II', 'eng_tsk_471', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (472, 'Engineering Guild Task: 1x Fuel Cell', 'eng_tsk_472', 1, 0, 0, 330, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 70);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (473, 'Engineering Guild Task: 1x Harvesting II', 'eng_tsk_473', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (474, 'Engineering Guild Task: 1x Hit Points II', 'eng_tsk_474', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (475, 'Engineering Guild Task: 1x HP Regen II', 'eng_tsk_475', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (476, 'Engineering Guild Task: 1x Improved Enmity II', 'eng_tsk_476', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (477, 'Engineering Guild Task: 1x Light Defense II', 'eng_tsk_477', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (478, 'Engineering Guild Task: 1x Light Potency II', 'eng_tsk_478', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (479, 'Engineering Guild Task: 1x Training Foil II (Blue)', 'eng_tsk_479', 1, 0, 0, 305, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (480, 'Engineering Guild Task: 1x Training Foil II (Green)', 'eng_tsk_480', 1, 0, 0, 305, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (481, 'Engineering Guild Task: 1x Training Foil II (Red)', 'eng_tsk_481', 1, 0, 0, 305, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (482, 'Engineering Guild Task: 1x Training Foil II (Yellow)', 'eng_tsk_482', 1, 0, 0, 305, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (483, 'Engineering Guild Task: 1x Lightsaber Repair Kit III', 'eng_tsk_483', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (484, 'Engineering Guild Task: 1x Mind Defense II', 'eng_tsk_484', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (485, 'Engineering Guild Task: 1x Mind Potency II', 'eng_tsk_485', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (486, 'Engineering Guild Task: 1x Reduced Enmity II', 'eng_tsk_486', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (487, 'Engineering Guild Task: 1x Resource Harvester III', 'eng_tsk_487', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (488, 'Engineering Guild Task: 1x Resource Scanner III', 'eng_tsk_488', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (489, 'Engineering Guild Task: 1x Training Foil Staff II (Blue)', 'eng_tsk_489', 1, 0, 0, 325, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 70);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (490, 'Engineering Guild Task: 1x Training Foil Staff II (Green)', 'eng_tsk_490', 1, 0, 0, 325, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 70);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (491, 'Engineering Guild Task: 1x Training Foil Staff II (Red)', 'eng_tsk_491', 1, 0, 0, 325, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 70);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (492, 'Engineering Guild Task: 1x Training Foil Staff II (Yellow)', 'eng_tsk_492', 1, 0, 0, 325, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 70);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (493, 'Engineering Guild Task: 1x Saberstaff Repair Kit III', 'eng_tsk_493', 1, 0, 0, 355, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 77);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (494, 'Engineering Guild Task: 1x Scanning Array (Small)', 'eng_tsk_494', 1, 0, 0, 340, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 73);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (495, 'Engineering Guild Task: 1x Sneak Attack II', 'eng_tsk_495', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (496, 'Engineering Guild Task: 1x Starship Auxiliary Blaster', 'eng_tsk_496', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (497, 'Engineering Guild Task: 1x Weaponsmith II', 'eng_tsk_497', 1, 0, 0, 310, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 65);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (498, 'Engineering Guild Task: 1x Activation Speed III', 'eng_tsk_498', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (499, 'Engineering Guild Task: 1x Armor Class III', 'eng_tsk_499', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (500, 'Engineering Guild Task: 1x Armorsmith III', 'eng_tsk_500', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (501, 'Engineering Guild Task: 1x Attack Bonus III', 'eng_tsk_501', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (502, 'Engineering Guild Task: 1x Auxiliary Shield Generator (Medium)', 'eng_tsk_502', 1, 0, 0, 460, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 98);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (503, 'Engineering Guild Task: 1x Auxiliary Targeter (Improved)', 'eng_tsk_503', 1, 0, 0, 450, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 95);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (504, 'Engineering Guild Task: 1x Auxiliary Thruster (Medium)', 'eng_tsk_504', 1, 0, 0, 510, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 111);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (505, 'Engineering Guild Task: 1x Base Attack Bonus III', 'eng_tsk_505', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (506, 'Engineering Guild Task: 1x Blaster Pistol IV', 'eng_tsk_506', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (507, 'Engineering Guild Task: 1x Blaster Pistol Repair Kit IV', 'eng_tsk_507', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (508, 'Engineering Guild Task: 1x Blaster Rifle IV', 'eng_tsk_508', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (509, 'Engineering Guild Task: 1x Blaster Rifle Repair Kit IV', 'eng_tsk_509', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (510, 'Engineering Guild Task: 1x Charisma III', 'eng_tsk_510', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (511, 'Engineering Guild Task: 1x Cloaking Generator (Medium)', 'eng_tsk_511', 1, 0, 0, 510, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 111);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (512, 'Engineering Guild Task: 1x Constitution III', 'eng_tsk_512', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (513, 'Engineering Guild Task: 1x Cooking III', 'eng_tsk_513', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (514, 'Engineering Guild Task: 1x Damage III', 'eng_tsk_514', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (515, 'Engineering Guild Task: 1x Dark Defense III', 'eng_tsk_515', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (516, 'Engineering Guild Task: 1x Dark Potency III', 'eng_tsk_516', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (517, 'Engineering Guild Task: 1x Dexterity III', 'eng_tsk_517', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (518, 'Engineering Guild Task: 1x Durability III', 'eng_tsk_518', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (519, 'Engineering Guild Task: 1x Electrical Defense III', 'eng_tsk_519', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (520, 'Engineering Guild Task: 1x Electrical Potency III', 'eng_tsk_520', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (521, 'Engineering Guild Task: 1x Engineering III', 'eng_tsk_521', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (522, 'Engineering Guild Task: 1x Enhancement Bonus III', 'eng_tsk_522', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (523, 'Engineering Guild Task: 1x Fabrication III', 'eng_tsk_523', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (524, 'Engineering Guild Task: 1x First Aid III', 'eng_tsk_524', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (525, 'Engineering Guild Task: 1x FP III', 'eng_tsk_525', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (526, 'Engineering Guild Task: 1x FP Regen III', 'eng_tsk_526', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (527, 'Engineering Guild Task: 1x Fuel Cell', 'eng_tsk_527', 1, 0, 0, 450, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 95);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (528, 'Engineering Guild Task: 1x Harvesting III', 'eng_tsk_528', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (529, 'Engineering Guild Task: 1x Hit Points III', 'eng_tsk_529', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (530, 'Engineering Guild Task: 1x HP Regen III', 'eng_tsk_530', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (531, 'Engineering Guild Task: 1x Hyperdrive', 'eng_tsk_531', 1, 0, 0, 490, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 107);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (532, 'Engineering Guild Task: 1x Improved Enmity III', 'eng_tsk_532', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (533, 'Engineering Guild Task: 1x Intelligence III', 'eng_tsk_533', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (534, 'Engineering Guild Task: 1x Light Defense III', 'eng_tsk_534', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (535, 'Engineering Guild Task: 1x Light Potency III', 'eng_tsk_535', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (536, 'Engineering Guild Task: 1x Light Starship Blaster', 'eng_tsk_536', 1, 0, 0, 470, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 101);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (537, 'Engineering Guild Task: 1x Training Foil III (Blue)', 'eng_tsk_537', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 85);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (538, 'Engineering Guild Task: 1x Training Foil III (Green)', 'eng_tsk_538', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 85);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (539, 'Engineering Guild Task: 1x Training Foil III (Red)', 'eng_tsk_539', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 85);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (540, 'Engineering Guild Task: 1x Training Foil III (Yellow)', 'eng_tsk_540', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 85);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (541, 'Engineering Guild Task: 1x Lightsaber Repair Kit IV', 'eng_tsk_541', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (542, 'Engineering Guild Task: 1x Mind Defense III', 'eng_tsk_542', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (543, 'Engineering Guild Task: 1x Mind Potency III', 'eng_tsk_543', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (544, 'Engineering Guild Task: 1x Reduced Enmity III', 'eng_tsk_544', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (545, 'Engineering Guild Task: 1x Resource Harvester IV', 'eng_tsk_545', 1, 0, 0, 410, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 85);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (546, 'Engineering Guild Task: 1x Resource Scanner IV', 'eng_tsk_546', 1, 0, 0, 410, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 85);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (547, 'Engineering Guild Task: 1x Training Foil Staff III (Blue)', 'eng_tsk_547', 1, 0, 0, 425, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (548, 'Engineering Guild Task: 1x Training Foil Staff III (Green)', 'eng_tsk_548', 1, 0, 0, 425, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (549, 'Engineering Guild Task: 1x Training Foil Staff III (Red)', 'eng_tsk_549', 1, 0, 0, 425, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (550, 'Engineering Guild Task: 1x Training Foil Staff III (Yellow)', 'eng_tsk_550', 1, 0, 0, 425, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (551, 'Engineering Guild Task: 1x Saberstaff Repair Kit IV', 'eng_tsk_551', 1, 0, 0, 455, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 97);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (552, 'Engineering Guild Task: 1x Scanning Array (Medium)', 'eng_tsk_552', 1, 0, 0, 510, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 111);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (553, 'Engineering Guild Task: 1x Sneak Attack III', 'eng_tsk_553', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (554, 'Engineering Guild Task: 1x Starship Auxiliary Light Cannon', 'eng_tsk_554', 1, 0, 0, 490, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 106);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (555, 'Engineering Guild Task: 1x Strength III', 'eng_tsk_555', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (556, 'Engineering Guild Task: 1x Weaponsmith III', 'eng_tsk_556', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (557, 'Engineering Guild Task: 1x Wisdom III', 'eng_tsk_557', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (558, 'Engineering Guild Task: 1x Training Foil IV (Blue)', 'eng_tsk_558', 1, 0, 0, 505, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 105);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (559, 'Engineering Guild Task: 1x Training Foil IV (Green)', 'eng_tsk_559', 1, 0, 0, 505, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 105);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (560, 'Engineering Guild Task: 1x Training Foil IV (Red)', 'eng_tsk_560', 1, 0, 0, 505, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 105);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (561, 'Engineering Guild Task: 1x Training Foil IV (Yellow)', 'eng_tsk_561', 1, 0, 0, 505, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 105);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (562, 'Engineering Guild Task: 1x Training Foil Staff IV (Blue)', 'eng_tsk_562', 1, 0, 0, 525, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 110);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (563, 'Engineering Guild Task: 1x Training Foil Staff IV (Green)', 'eng_tsk_563', 1, 0, 0, 525, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 110);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (564, 'Engineering Guild Task: 1x Training Foil Staff IV (Red)', 'eng_tsk_564', 1, 0, 0, 525, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 110);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (565, 'Engineering Guild Task: 1x Training Foil Staff IV (Yellow)', 'eng_tsk_565', 1, 0, 0, 525, NULL, 0, 1, '',NULL, 0, '','','','','','','','',2, 110);

INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (349, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (349, 'blaster_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (350, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (350, 'rifle_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (351, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (351, 'scanner_m_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (352, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (352, 'harvest_r_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (353, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (353, 'scanner_r_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (354, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (354, 'c_cluster_blue', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (355, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (355, 'fuel_cell', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (356, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (356, 'c_cluster_green', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (357, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (357, 'pistol_barrel', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (358, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (358, 'c_cluster_power', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (359, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (359, 'r_weapon_core', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (360, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (360, 'c_cluster_red', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (361, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (361, 'rifle_barrel', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (362, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (362, 'c_cluster_yellow', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (363, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (363, 'rune_cstspd1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (364, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (364, 'rune_ac1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (365, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (365, 'rune_armsmth1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (366, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (366, 'rune_ab1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (367, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (367, 'lightsaber_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (368, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (368, 'lightsaber_g_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (369, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (369, 'lightsaber_r_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (370, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (370, 'lightsaber_y_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (371, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (371, 'saberstaff_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (372, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (372, 'saberstaff_g_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (373, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (373, 'saberstaff_r_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (374, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (374, 'saberstaff_y_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (375, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (375, 'blaster_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (376, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (376, 'bp_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (377, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (377, 'rifle_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (378, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (378, 'br_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (379, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (379, 'rune_cha1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (380, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (380, 'rune_con1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (381, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (381, 'rune_cooking1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (382, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (382, 'rune_dmg1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (383, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (383, 'rune_ddef1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (384, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (384, 'rune_evo1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (385, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (385, 'rune_dex1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (386, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (386, 'rune_edef1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (387, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (387, 'rune_ele1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (388, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (388, 'emitter', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (389, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (389, 'rune_engin1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (390, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (390, 'rune_fab1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (391, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (391, 'rune_faid1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (392, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (392, 'rune_mana1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (393, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (393, 'fuel_cell', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (394, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (394, 'rune_mining1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (395, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (395, 'rune_hp1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (396, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (396, 'rune_enmup1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (397, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (397, 'rune_int1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (398, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (398, 'rune_ldef1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (399, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (399, 'rune_alt1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (400, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (400, 'ls_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (401, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (401, 'rune_mdef1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (402, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (402, 'rune_sum1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (403, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (403, 'harvest_r_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (404, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (404, 'scanner_r_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (405, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (405, 'ls_hilt', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (406, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (406, 'ss_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (407, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (407, 'rune_snkatk1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (408, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (408, 'rune_str1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (409, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (409, 'rune_wpnsmth1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (410, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (410, 'rune_wis1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (411, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (411, 'rune_cstspd2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (412, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (412, 'rune_ab2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (413, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (413, 'rune_bab1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (414, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (414, 'blaster_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (415, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (415, 'bp_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (416, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (416, 'rifle_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (417, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (417, 'br_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (418, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (418, 'rune_cha2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (419, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (419, 'rune_con2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (420, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (420, 'rune_dex2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (421, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (421, 'rune_dur1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (422, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (422, 'rune_eb1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (423, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (423, 'rune_manareg1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (424, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (424, 'fuel_cell', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (425, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (425, 'rune_hpregen1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (426, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (426, 'rune_int2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (427, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (427, 'rune_lvldown1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (428, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (428, 'rune_lvlup1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (429, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (429, 'lightsaber_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (430, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (430, 'lightsaber_g_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (431, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (431, 'lightsaber_r_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (432, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (432, 'lightsaber_y_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (433, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (433, 'ls_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (434, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (434, 'rune_luck1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (435, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (435, 'rune_luck2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (436, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (436, 'rune_med1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (437, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (437, 'rune_med2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (438, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (438, 'rune_enmdown1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (439, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (439, 'harvest_r_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (440, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (440, 'scanner_r_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (441, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (441, 'saberstaff_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (442, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (442, 'saberstaff_g_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (443, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (443, 'saberstaff_r_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (444, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (444, 'saberstaff_y_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (445, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (445, 'ss_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (446, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (446, 'rune_str2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (447, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (447, 'rune_wis2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (448, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (448, 'rune_ac2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (449, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (449, 'rune_armsmth2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (450, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (450, 'ssshld1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (451, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (451, 'ssrang1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (452, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (452, 'ssspd1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (453, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (453, 'rune_bab2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (454, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (454, 'blaster_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (455, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (455, 'bp_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (456, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (456, 'rifle_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (457, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (457, 'br_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (458, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (458, 'ssstlth1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (459, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (459, 'rune_cooking2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (460, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (460, 'rune_dmg2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (461, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (461, 'rune_ddef2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (462, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (462, 'rune_evo2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (463, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (463, 'rune_dur2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (464, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (464, 'rune_edef2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (465, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (465, 'rune_ele2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (466, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (466, 'rune_engin2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (467, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (467, 'rune_eb2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (468, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (468, 'rune_fab2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (469, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (469, 'rune_faid2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (470, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (470, 'rune_mana2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (471, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (471, 'rune_manareg2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (472, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (472, 'fuel_cell', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (473, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (473, 'rune_mining2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (474, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (474, 'rune_hp2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (475, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (475, 'rune_hpregen2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (476, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (476, 'rune_enmup2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (477, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (477, 'rune_ldef2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (478, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (478, 'rune_alt2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (479, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (479, 'lightsaber_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (480, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (480, 'lightsaber_g_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (481, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (481, 'lightsaber_r_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (482, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (482, 'lightsaber_y_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (483, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (483, 'ls_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (484, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (484, 'rune_mdef2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (485, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (485, 'rune_sum2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (486, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (486, 'rune_enmdown2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (487, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (487, 'harvest_r_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (488, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (488, 'scanner_r_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (489, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (489, 'saberstaff_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (490, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (490, 'saberstaff_g_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (491, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (491, 'saberstaff_r_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (492, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (492, 'saberstaff_y_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (493, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (493, 'ss_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (494, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (494, 'ssscan1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (495, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (495, 'rune_snkatk2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (496, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (496, 'sswpn1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (497, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (497, 'rune_wpnsmth2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (498, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (498, 'rune_cstspd3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (499, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (499, 'rune_ac3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (500, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (500, 'rune_armsmth3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (501, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (501, 'rune_ab3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (502, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (502, 'ssshld2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (503, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (503, 'ssrang2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (504, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (504, 'ssspd2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (505, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (505, 'rune_bab3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (506, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (506, 'blaster_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (507, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (507, 'bp_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (508, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (508, 'rifle_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (509, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (509, 'br_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (510, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (510, 'rune_cha3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (511, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (511, 'ssstlth2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (512, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (512, 'rune_con3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (513, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (513, 'rune_cooking3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (514, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (514, 'rune_dmg3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (515, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (515, 'rune_ddef3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (516, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (516, 'rune_evo3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (517, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (517, 'rune_dex3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (518, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (518, 'rune_dur3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (519, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (519, 'rune_edef3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (520, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (520, 'rune_ele3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (521, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (521, 'rune_engin3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (522, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (522, 'rune_eb3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (523, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (523, 'rune_fab3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (524, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (524, 'rune_faid3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (525, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (525, 'rune_mana3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (526, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (526, 'rune_manareg3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (527, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (527, 'fuel_cell', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (528, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (528, 'rune_mining3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (529, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (529, 'rune_hp3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (530, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (530, 'rune_hpregen3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (531, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (531, 'hyperdrive', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (532, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (532, 'rune_enmup3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (533, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (533, 'rune_int3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (534, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (534, 'rune_ldef3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (535, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (535, 'rune_alt3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (536, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (536, 'ship_blaster_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (537, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (537, 'lightsaber_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (538, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (538, 'lightsaber_g_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (539, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (539, 'lightsaber_r_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (540, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (540, 'lightsaber_y_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (541, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (541, 'ls_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (542, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (542, 'rune_mdef3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (543, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (543, 'rune_sum3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (544, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (544, 'rune_enmdown3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (545, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (545, 'harvest_r_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (546, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (546, 'scanner_r_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (547, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (547, 'saberstaff_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (548, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (548, 'saberstaff_g_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (549, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (549, 'saberstaff_r_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (550, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (550, 'saberstaff_y_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (551, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (551, 'ss_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (552, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (552, 'ssscan2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (553, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (553, 'rune_snkatk3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (554, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (554, 'sswpn2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (555, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (555, 'rune_str3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (556, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (556, 'rune_wpnsmth3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (557, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (557, 'rune_wis3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (558, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (558, 'lightsaber_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (559, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (559, 'lightsaber_g_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (560, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (560, 'lightsaber_r_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (561, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (561, 'lightsaber_y_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (562, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (562, 'saberstaff_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (563, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (563, 'saberstaff_g_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (564, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (564, 'saberstaff_r_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (565, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (565, 'saberstaff_y_4', 1, SCOPE_IDENTITY(), 1);

INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 349, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 350, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 351, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 352, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 353, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 354, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 355, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 356, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 357, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 358, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 359, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 360, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 361, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 362, 0, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 363, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 364, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 365, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 366, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 367, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 368, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 369, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 370, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 371, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 372, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 373, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 374, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 375, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 376, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 377, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 378, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 379, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 380, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 381, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 382, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 383, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 384, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 385, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 386, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 387, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 388, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 389, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 390, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 391, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 392, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 393, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 394, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 395, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 396, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 397, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 398, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 399, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 400, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 401, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 402, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 403, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 404, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 405, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 406, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 407, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 408, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 409, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 410, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 411, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 412, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 413, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 414, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 415, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 416, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 417, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 418, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 419, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 420, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 421, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 422, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 423, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 424, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 425, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 426, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 427, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 428, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 429, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 430, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 431, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 432, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 433, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 434, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 435, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 436, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 437, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 438, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 439, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 440, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 441, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 442, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 443, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 444, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 445, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 446, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 447, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 448, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 449, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 450, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 451, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 452, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 453, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 454, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 455, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 456, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 457, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 458, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 459, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 460, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 461, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 462, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 463, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 464, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 465, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 466, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 467, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 468, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 469, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 470, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 471, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 472, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 473, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 474, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 475, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 476, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 477, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 478, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 479, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 480, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 481, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 482, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 483, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 484, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 485, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 486, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 487, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 488, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 489, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 490, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 491, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 492, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 493, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 494, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 495, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 496, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 497, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 498, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 499, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 500, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 501, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 502, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 503, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 504, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 505, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 506, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 507, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 508, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 509, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 510, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 511, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 512, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 513, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 514, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 515, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 516, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 517, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 518, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 519, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 520, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 521, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 522, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 523, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 524, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 525, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 526, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 527, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 528, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 529, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 530, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 531, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 532, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 533, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 534, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 535, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 536, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 537, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 538, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 539, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 540, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 541, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 542, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 543, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 544, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 545, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 546, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 547, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 548, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 549, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 550, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 551, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 552, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 553, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 554, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 555, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 556, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 557, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 558, 99, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 559, 99, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 560, 99, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 561, 99, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 562, 99, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 563, 99, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 564, 99, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (2, 565, 99, 0);

-- ENGINEERING INSERTS END




-- Remove disabled crafted items

DELETE FROM dbo.QuestRequiredItem
WHERE QuestID IN (383,384,386,387,398,399,401,402,453,461,462,464,465,477,478,484,485,505,515,516,519,520,534,535,542,543)

DELETE FROM dbo.QuestState
WHERE QuestID IN (383,384,386,387,398,399,401,402,453,461,462,464,465,477,478,484,485,505,515,516,519,520,534,535,542,543)

DELETE FROM dbo.GuildTask
WHERE QuestID IN (383,384,386,387,398,399,401,402,453,461,462,464,465,477,478,484,485,505,515,516,519,520,534,535,542,543)

DELETE FROM dbo.Quest
WHERE ID IN (383,384,386,387,398,399,401,402,453,461,462,464,465,477,478,484,485,505,515,516,519,520,534,535,542,543)

GO


--  Why: To avoid divide by zero error in AbilityService.ProcessConcentrationEffects:
--                 // Are we ready to continue processing this concentration effect?
--                if (tick % perkFeat.ConcentrationTickInterval != 0) return;
--  What: Set all concentration based perks that have ConcentrationTickInterval = 0 
--        to ConcentrationTickInterval = 1 to avoid divide by zero error.
update PerkFeat
    set ConcentrationTickInterval = 1
    where PerkID in (select p.id
                        from Perk p
                        inner join PerkFeat pf on pf.perkid = p.id
                        where exists (select 1 from PerkCategory pc
                                        where upper(pc.name) like '%FORCE%'
                                        and pc.IsActive = 1
                                        and pc.id = p.PerkCategoryID)
                        and pf.ConcentrationTickInterval = 0
                        and p.ExecutionTypeID = (select pet.id
                                                    from PerkExecutionType pet
                                                    where upper(pet.Name) like '%CONCENTRATION%'));
                                                    

GO


-- Consolidate perk categories for force.
UPDATE dbo.PerkCategory
SET Name = 'Force Alter'
WHERE ID = 40

UPDATE dbo.PerkCategory
SET Name = 'Force Control'
WHERE ID = 43

UPDATE dbo.PerkCategory
SET Name = 'Force Sense'
WHERE ID = 46

UPDATE dbo.Perk
SET PerkCategoryID = 40
WHERE PerkCategoryID IN (41, 42, 49)

UPDATE dbo.Perk
SET PerkCategoryID = 43
WHERE PerkCategoryID IN (44, 45, 50)

UPDATE dbo.Perk
SET PerkCategoryID = 46
WHERE PerkCategoryID IN (47, 48, 51)

DELETE FROM dbo.PerkCategory
WHERE ID IN (41, 42, 49, 44, 45, 50, 47, 48, 51)


-- Fix Rage's interval to 6 seconds.
UPDATE dbo.PerkFeat
SET ConcentrationTickInterval = 6
WHERE PerkID = 19


-- Fix description of Force Speed.
UPDATE dbo.PerkLevel
SET Description = 'Increases movement speed by 50%, Dexterity by 10 and grants an extra attack.'
WHERE PerkID = 3
	AND Level = 5


-- Reduce tick interval for force lightning to 3 per spreadsheet changes.
UPDATE dbo.PerkFeat
SET ConcentrationTickInterval = 3
WHERE PerkID = 182


-- Move FP costs to concentration and bump interval to 6 for Force Stun.
UPDATE dbo.PerkFeat
SET ConcentrationFPCost = BaseFPCost,
	BaseFPCost = 0,
	ConcentrationTickInterval = 6
WHERE PerkID = 126


GO


INSERT INTO dbo.Skill ( ID ,
                        SkillCategoryID ,
                        Name ,
                        MaxRank ,
                        IsActive ,
                        Description ,
                        [Primary] ,
                        Secondary ,
                        Tertiary ,
                        ContributesToSkillCap )
VALUES ( 38 ,    -- ID - int
         8 ,    -- SkillCategoryID - int
         N'Ugnaught' ,  -- Name - nvarchar(32)
         20 ,    -- MaxRank - int
         1 , -- IsActive - bit
         N'Ability to speak the Ugnaught language.' ,  -- Description - nvarchar(1024)
         0 ,    -- Primary - int
         0 ,    -- Secondary - int
         0 ,    -- Tertiary - int
         0   -- ContributesToSkillCap - bit
    )

GO


DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)
)

DELETE FROM dbo.PerkLevelQuestRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)
)

DELETE FROM dbo.PCPerkRefund
WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)

DELETE FROM dbo.PerkLevel
WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)

DELETE FROM dbo.Perk
WHERE ID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)




INSERT INTO dbo.Perk ( ID ,
                       Name ,
                       IsActive ,
                       BaseCastingTime ,
                       Description ,
                       PerkCategoryID ,
                       CooldownCategoryID ,
                       ExecutionTypeID ,
                       IsTargetSelfOnly ,
                       Enmity ,
                       EnmityAdjustmentRuleID ,
                       CastAnimationID ,
                       ForceBalanceTypeID )
VALUES ( 38 ,    -- ID - int
         'Guild Relations' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         0.0 ,  -- BaseCastingTime - float
         N'Improves your GP acquisition with all guilds.' ,  -- Description - nvarchar(256)
         4 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         0 ,    -- CastAnimationID - int
         0      -- ForceBalanceTypeID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         1 ,   -- Level - int
         6 ,   -- Price - int
         N'Doubles GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    ) 

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         2 ,   -- Level - int
         6 ,   -- Price - int
         N'Triples GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         3 ,   -- Level - int
         6 ,   -- Price - int
         N'Quadruples GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )





INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 16 , -- ID - int
         N'Viscara Crystal Spider' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 17 , -- ID - int
         N'Mon Cala Aradile' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 18 , -- ID - int
         N'Mon Cala Viper' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 19 , -- ID - int
         N'Mon Cala Amphi-Hydrus' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 20 , -- ID - int
         N'Mon Cala Eco Terrorist' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 21 , -- ID - int
         N'Vellen Flesheater' -- Name - nvarchar(32)
    )

DECLARE @GuildID INT = 1
DECLARE @QuestID INT = 566
DECLARE @Name NVARCHAR(128)
DECLARE @Quantity INT 
DECLARE @GP INT 
DECLARE @Gold INT 
DECLARE @NPCGroupID INT
DECLARE @RequiredRank INT 
DECLARE @Resref NVARCHAR(16)

------------
-- RANK 0 --
------------

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x CZ-220 Mynock'
SET @GP = 7
SET @Gold = 20
SET @NPCGroupID = 1
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x CZ-220 Malfunctioning Droid'
SET @GP = 7
SET @Gold = 37
SET @NPCGroupID = 2
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , 0 , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 1
SET @Name = '1x CZ-220 Colicoid Experiment'
SET @GP = 15
SET @Gold = 53
SET @NPCGroupID = 3
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Viscara Kath Hound'
SET @GP = 12
SET @Gold = 65
SET @NPCGroupID = 4
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Viscara Warocas'
SET @GP = 12
SET @Gold = 65
SET @NPCGroupID = 14
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Wildwoods Outlaw'
SET @GP = 12
SET @Gold = 65
SET @NPCGroupID = 8
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 8
SET @Name = '8x Wildwoods Gimpassa'
SET @GP = 13
SET @Gold = 70
SET @NPCGroupID = 9
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Wildwoods Kinrath'
SET @GP = 13
SET @Gold = 70
SET @NPCGroupID = 10
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mynock Wing'
SET @GP = 7
SET @Gold = 23
SET @Resref = 'mynock_wing'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0) 
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mynock Tooth'
SET @GP = 7
SET @Gold = 23
SET @Resref = 'mynock_tooth'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mynock Wing'
SET @GP = 7
SET @Gold = 23
SET @Resref = 'mynock_wing'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mynock Tooth'
SET @GP = 7
SET @Gold = 23
SET @Resref = 'mynock_tooth'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Kath Hound Fur'
SET @GP = 12
SET @Gold = 65
SET @Resref = 'k_hound_fur'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Kath Hound Meat'
SET @GP = 12
SET @Gold = 65
SET @Resref = 'kath_meat_1'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Kath Hound Tooth'
SET @GP = 14
SET @Gold = 67
SET @Resref = 'k_hound_tooth'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Warocas Leg'
SET @GP = 14
SET @Gold = 67
SET @Resref = 'waro_leg'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Warocas Meat'
SET @GP = 14
SET @Gold = 67
SET @Resref = 'warocas_meat'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Warocas Spine'
SET @GP = 15
SET @Gold = 68
SET @Resref = 'waro_feathers'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)




------------
-- RANK 1 --
------------


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mandalorian Warrior'
SET @GP = 19
SET @Gold = 76
SET @NPCGroupID = 6
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mandalorian Ranger'
SET @GP = 19
SET @Gold = 76
SET @NPCGroupID = 7
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 1
SET @Name = '1x Mandalorian Leader'
SET @GP = 24
SET @Gold = 82
SET @NPCGroupID = 5
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Valley Cairnmog'
SET @GP = 27
SET @Gold = 84
SET @NPCGroupID = 11
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Valley Raivor'
SET @GP = 27
SET @Gold = 84
SET @NPCGroupID = 13
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Valley Nashtah'
SET @GP = 27
SET @Gold = 84
SET @NPCGroupID = 15
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalore Herb'
SET @GP = 19
SET @Gold = 76
SET @Resref = 'herb_m'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Dog Tags'
SET @GP = 20
SET @Gold = 80
SET @Resref = 'man_tags'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Plexi-plate'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_plexiplate'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Blaster Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_blast_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Large Vibroblade Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_lvibro_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Lightsaber Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_ls_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Polearm Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_polearm_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Vibroblade Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_vibro_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

------------
-- RANK 2 --
------------

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Damaged Blue Crystal'
SET @GP = 39
SET @Gold = 122
SET @Resref = 'p_crystal_blue'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Damaged Green Crystal'
SET @GP = 39
SET @Gold = 122
SET @Resref = 'p_crystal_green'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Damaged Red Crystal'
SET @GP = 39
SET @Gold = 122
SET @Resref = 'p_crystal_red'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Damaged Yellow Crystal'
SET @GP = 39
SET @Gold = 122
SET @Resref = 'p_crystal_yellow'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Crystal Spider'
SET @GP = 39
SET @Gold = 122
SET @NPCGroupID = 16
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mon Cala Aradile'
SET @GP = 52
SET @Gold = 212
SET @NPCGroupID = 17
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mon Cala Viper'
SET @GP = 52
SET @Gold = 212
SET @NPCGroupID = 18
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mon Cala Amphi-Hydrus'
SET @GP = 52
SET @Gold = 212
SET @NPCGroupID = 19
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Amphi-Hydrus Brain Stem'
SET @GP = 52
SET @Gold = 212
SET @Resref = 'amphi_brain2'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Amphi-Hydrus Brain'
SET @GP = 52
SET @Gold = 212
SET @Resref = 'amphi_brain'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 1
SET @Name = '1x Vellen Fleshleader'
SET @GP = 44
SET @Gold = 184
SET @NPCGroupID = 12
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Vellen Flesheater'
SET @GP = 44
SET @Gold = 184
SET @NPCGroupID = 21
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


GO


INSERT INTO dbo.CooldownCategory ( ID ,
                                   Name ,
                                   BaseCooldownTime )
VALUES ( 40 ,   -- ID - int
         N'Skewer' , -- Name - nvarchar(64)
         300.0   -- BaseCooldownTime - float
    )

INSERT INTO dbo.Perk ( ID ,
                       Name ,
                       IsActive ,
                       BaseCastingTime ,
                       Description ,
                       PerkCategoryID ,
                       CooldownCategoryID ,
                       ExecutionTypeID ,
                       IsTargetSelfOnly ,
                       Enmity ,
                       EnmityAdjustmentRuleID ,
                       CastAnimationID ,
                       ForceBalanceTypeID )
VALUES ( 113 ,    -- ID - int
         'Skewer' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         0.0 ,  -- BaseCastingTime - float
         N'Interrupts your target''s concentration effect. Must be equipped with a Polearm.' ,  -- Description - nvarchar(256)
         15 ,    -- PerkCategoryID - int
         40 ,    -- CooldownCategoryID - int
         2 ,    -- ExecutionTypeID - int
         1 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL ,    -- CastAnimationID - int
         0      -- ForceBalanceTypeID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 113 ,   -- PerkID - int
         1 ,   -- Level - int
         4 ,   -- Price - int
         N'25% chance to interrupt' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         2 , -- SkillID - int
         15   -- RequiredRank - int
    )

INSERT INTO dbo.PerkFeat ( PerkID ,
                           FeatID ,
                           PerkLevelUnlocked ,
                           BaseFPCost ,
                           ConcentrationFPCost ,
                           ConcentrationTickInterval )
VALUES ( 113 , -- PerkID - int
         1247 , -- FeatID - int
         1 , -- PerkLevelUnlocked - int
         0 , -- BaseFPCost - int
         0 , -- ConcentrationFPCost - int
         0   -- ConcentrationTickInterval - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 113 ,   -- PerkID - int
         2 ,   -- Level - int
         4 ,   -- Price - int
         N'50% chance to interrupt' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         2 , -- SkillID - int
         30   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 113 ,   -- PerkID - int
         3 ,   -- Level - int
         5 ,   -- Price - int
         N'75% chance to interrupt' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         2 , -- SkillID - int
         50   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 113 ,   -- PerkID - int
         4 ,   -- Level - int
         6 ,   -- Price - int
         N'100% chance to interrupt' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         2 , -- SkillID - int
         80   -- RequiredRank - int
    )

GO



UPDATE dbo.LootTableItem
SET SpawnRule = ''
WHERE SpawnRule = 'DrillSpawnRule'



UPDATE dbo.CooldownCategory
SET BaseCooldownTime = 300
WHERE ID = 32 -- 32 = Chi

GO


-- Merge all of the mod installation perks into one, per skill.

-- Refund player perks

EXEC dbo.ADM_RefundPlayerPerk @PerkID = 83 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 86 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 88 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 97 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 109 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 110 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 111 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 112 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 156 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 157 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 158 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 159 -- int



-- Combat Mod Installation - Weapons becomes Weapon Mod Installation
UPDATE dbo.Perk
SET Name = 'Weapon Mod Installation',
	Description = 'Enables the installation of mods into weapons.'
WHERE ID = 83

UPDATE dbo.PerkLevel
SET Description = REPLACE(Description, 'red ', '')
WHERE PerkID = 83

-- Combat Mod Installation - Armors becomes Armor Mod Installation
UPDATE dbo.Perk
SET Name = 'Armor Mod Installation',
	Description = 'Enables the installation of mods into armors.'
WHERE ID = 109

UPDATE dbo.PerkLevel
SET Description = REPLACE(Description, 'red ', '')
WHERE PerkID = 109

-- Combat Mod Installation - Electronics becomes Engineering Mod Installation
UPDATE dbo.Perk
SET Name = 'Engineering Mod Installation',
	Description = 'Enables the installation of mods into engineering items.'
WHERE ID = 156

UPDATE dbo.PerkLevel
SET Description = REPLACE(Description, 'red ', '')
WHERE PerkID = 156


-- Remove the excess perks.
DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (86,88 ,97 ,110,111,112,157,158,159) 
)

DELETE FROM dbo.PerkLevel
WHERE PerkID IN (86,88 ,97 ,110,111,112,157,158,159) 

DELETE FROM dbo.PCPerkRefund
WHERE PerkID IN (86,88 ,97 ,110,111,112,157,158,159) 

DELETE FROM dbo.Perk
WHERE ID IN (86,88 ,97 ,110,111,112,157,158,159) 

-- Rename Battle Meditation -> Battle Insight
UPDATE dbo.Perk
SET Name = 'Battle Insight'
WHERE ID = 179


-- Mind Shield: Lower cooldown to 5 minutes
UPDATE dbo.CooldownCategory
SET BaseCooldownTime = '300'
WHERE ID = 6


-- Drop Force Lightning & Drain Life cooldowns to 12 seconds.
UPDATE dbo.CooldownCategory
SET BaseCooldownTime = 12
WHERE ID = 36

UPDATE dbo.CooldownCategory
SET BaseCooldownTime = 12
WHERE ID = 35


-- Drop Force Breach cooldown to 10 seconds.
UPDATE dbo.CooldownCategory
SET BaseCooldownTime = 10
WHERE ID = 38


-- Drop Throw Saber cooldown to 10 seconds.
UPDATE dbo.CooldownCategory
SET BaseCooldownTime = 10
WHERE ID = 12


GO

-- Remove duplicate guild task quests

DELETE FROM dbo.PCQuestItemProgress
WHERE PCQuestStatusID IN (
	SELECT ID
	FROM dbo.PCQuestStatus
	WHERE QuestID IN (355,393,424,472,527,577,578)
)
DELETE FROM dbo.PCQuestStatus
WHERE QuestID IN (355,393,424,472,527,577,578)
DELETE FROM dbo.QuestRequiredItem 
WHERE QuestID IN (355,393,424,472,527,577,578)

DELETE FROM dbo.QuestState
WHERE QuestID IN (355,393,424,472,527,577,578)

DELETE FROM dbo.GuildTask
WHERE QuestID IN (355,393,424,472,527,577,578)

DELETE FROM dbo.Quest
WHERE ID IN (355,393,424,472,527,577,578)



-- Move AC mods to yellow category and require yellow clusters.

UPDATE dbo.CraftBlueprint
SET CraftCategoryID = 17,
	MainComponentTypeID = 31
WHERE ID IN (127, 160, 190)


-- Fix the FP cost on Force Heal perk feats
UPDATE dbo.PerkFeat
SET ConcentrationFPCost = 2 
WHERE PerkID = 185 AND FeatID = 1176

UPDATE dbo.PerkFeat
SET ConcentrationFPCost = 3 
WHERE PerkID = 185 AND FeatID = 1177

UPDATE dbo.PerkFeat
SET ConcentrationFPCost = 4 
WHERE PerkID = 185 AND FeatID = 1178

UPDATE dbo.PerkFeat
SET ConcentrationFPCost = 5
WHERE PerkID = 185 AND FeatID = 1179


-- Add the Force Insight perk.
UPDATE dbo.CooldownCategory
SET Name = 'Battle/Force Insight'
WHERE ID = 28

INSERT INTO dbo.Perk ( ID ,
                       Name ,
                       IsActive ,
                       BaseCastingTime ,
                       Description ,
                       PerkCategoryID ,
                       CooldownCategoryID ,
                       ExecutionTypeID ,
                       IsTargetSelfOnly ,
                       Enmity ,
                       EnmityAdjustmentRuleID ,
                       CastAnimationID ,
                       ForceBalanceTypeID )
VALUES ( 86 ,    -- ID - int
         'Force Insight' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         0.0 ,  -- BaseCastingTime - float
         N'The caster boosts their AB and AC. Only affects themselves.' ,  -- Description - nvarchar(256)
         46 ,    -- PerkCategoryID - int
         28 ,    -- CooldownCategoryID - int
         7 ,    -- ExecutionTypeID - int
         1 , -- IsTargetSelfOnly - bit
         25 ,    -- Enmity - int
         2 ,    -- EnmityAdjustmentRuleID - int
         NULL ,    -- CastAnimationID - int
         0      -- ForceBalanceTypeID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 86 ,   -- PerkID - int
         1 ,   -- Level - int
         3 ,   -- Price - int
         N'Caster gets +3 AB.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )
INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         21 , -- SkillID - int
         0   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 86 ,   -- PerkID - int
         2 ,   -- Level - int
         4 ,   -- Price - int
         N'Caster gets +5 AB and +2 AC.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         21 , -- SkillID - int
         15   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 86 ,   -- PerkID - int
         3 ,   -- Level - int
         5 ,   -- Price - int
         N'Caster gets +5 AB and +4 AC.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         21 , -- SkillID - int
         30   -- RequiredRank - int
    )

INSERT INTO dbo.PerkFeat ( PerkID ,
                           FeatID ,
                           PerkLevelUnlocked ,
                           BaseFPCost ,
                           ConcentrationFPCost ,
                           ConcentrationTickInterval )
VALUES ( 86 , -- PerkID - int
         1248 , -- FeatID - int
         1 , -- PerkLevelUnlocked - int
         0 , -- BaseFPCost - int
         1 , -- ConcentrationFPCost - int
         1   -- ConcentrationTickInterval - int
    )


INSERT INTO dbo.PerkFeat ( PerkID ,
                           FeatID ,
                           PerkLevelUnlocked ,
                           BaseFPCost ,
                           ConcentrationFPCost ,
                           ConcentrationTickInterval )
VALUES ( 86 , -- PerkID - int
         1249 , -- FeatID - int
         2 , -- PerkLevelUnlocked - int
         0 , -- BaseFPCost - int
         2 , -- ConcentrationFPCost - int
         1   -- ConcentrationTickInterval - int
    )


INSERT INTO dbo.PerkFeat ( PerkID ,
                           FeatID ,
                           PerkLevelUnlocked ,
                           BaseFPCost ,
                           ConcentrationFPCost ,
                           ConcentrationTickInterval )
VALUES ( 86 , -- PerkID - int
         1250 , -- FeatID - int
         3 , -- PerkLevelUnlocked - int
         0 , -- BaseFPCost - int
         3 , -- ConcentrationFPCost - int
         1   -- ConcentrationTickInterval - int
    )



	
-- Fix Force Sense description
UPDATE dbo.Skill
SET Description = 'Ability to use sense-based force abilities like Force Insight and Premonition. Higher skill levels unlock new abilities.'
WHERE ID = 21



INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 22 ,    -- LootTableID - int
         'greatsword_mando' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         1 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )


-- Fix Throw Saber description.
UPDATE dbo.PerkLevel
SET Description = 'Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 100%.  This ability hits automatically.'
WHERE PerkID = 174 AND Level = 1
UPDATE dbo.PerkLevel
SET Description = 'Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 125%.  This ability hits automatically.'
WHERE PerkID = 174 AND Level = 2

-- Decrease skill level requirements for throw saber by 10. Gives players a low-level alter ability to use.
UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = RequiredRank - 10
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID = 174 
)


-- Change stat increase amount on mods.
UPDATE dbo.CraftBlueprint
SET ItemName = REPLACE(REPLACE(REPLACE(ItemName, '+3', '+1'), '+6', '+2'), '+9', '+3')
WHERE ItemResref LIKE 'rune_%'
	AND (ItemName LIKE '%+3%'  OR
		ItemName LIKE '%+6%' OR 
		ItemName LIKE '%+9%')


		
-- Refund Optimization Perks
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 143 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 144 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 145 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 146 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 147 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 148 -- int


-- Refund Efficiency Perks
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 137 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 138 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 139 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 140 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 141 -- int
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 142 -- int

DELETE FROM dbo.PCPerkRefund
WHERE PerkID IN (143, 144, 145, 146, 147, 148, 137, 138, 139, 140, 141, 142) 

DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (143, 144, 145, 146, 147, 148, 137, 138, 139, 140, 141, 142) 
)

DELETE FROM dbo.PerkLevel
WHERE PerkID IN (143, 144, 145, 146, 147, 148, 137, 138, 139, 140, 141, 142) 

DELETE FROM dbo.Perk
WHERE ID IN (143, 144, 145, 146, 147, 148, 137, 138, 139, 140, 141, 142) 



-- Remove the level up mods.
DELETE FROM dbo.PCCraftedBlueprint
WHERE CraftBlueprintID = 146

DELETE FROM dbo.CraftBlueprint
WHERE ID = 146

UPDATE dbo.CraftBlueprint
SET ItemName = 'Level Decrease -3'
WHERE ID = 147


-- Reduce crystal cluster requirements to 4 items instead of 6.
UPDATE dbo.CraftBlueprint
SET MainMinimum = 4, MainMaximum = 4
WHERE ID IN (113, 114, 115, 116)
