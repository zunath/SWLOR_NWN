
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
ADD SpecializationID INT NOT NULL DEFAULT 0
CONSTRAINT FK_Perk_SpecializationID FOREIGN KEY REFERENCES dbo.Specialization(ID)

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
( 37 , N'Force Push' , 600.0),
( 38 , N'Force Breach' , 600.0),
( 39 , N'Force Heal' , 600.0)


-- Definitions for each perk
INSERT INTO dbo.Perk (ID, PerkCategoryID, CooldownCategoryID, ExecutionTypeID, IsTargetSelfOnly, Name, IsActive, Description, Enmity, EnmityAdjustmentRuleID, ForceBalanceTypeID) VALUES
 (3, 45, 2, 3, 1, 'Force Speed', 1, 'Increases movement speed and dexterity.  At higher ranks grants additional attacks.', 0, 0, 0),
 (4, 46, 4, 3, 1, 'Absorb Energy', 1, 'Absorbs a percentage of damage that the caster would take, from all sources.', 20, 2, 0),
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
 (185, 45, 39, 3, 0, 'Force Heal', 1, 'Restores HP on a single target over time.', 0, 0, 1)
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
 (185,2,2, 'Heals a single target for 3 HP every second.', 0, 1),
 (185,3,3, 'Heals a single target for 5 HP every second.', 0, 1),
 (185,4,3, 'Heals a single target for 7 HP every second.', 2, 1),
 (185,5,4, 'Heals a single target for 10 HP every second.', 2, 1),
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
  (@PerkLevelID-64,19,0),
  (@PerkLevelID-63,19,10),
  (@PerkLevelID-62,19,20),
  (@PerkLevelID-61,19,30),
  (@PerkLevelID-60,19,40),

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
