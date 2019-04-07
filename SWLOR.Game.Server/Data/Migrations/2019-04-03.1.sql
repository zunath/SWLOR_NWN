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

-- Definitions for each perk
INSERT INTO dbo.Perk (ID, PerkCategoryID, CooldownCategoryID, ExecutionTypeID, IsTargetSelfOnly, Name, IsActive, Description, Emnity, EnmityAdjustmentRuleID, ExecutionTypeID, ForceBalanceTypeID) VALUES
 (172, 45, 34, 3, 1, 'Force Speed', 1, 'Increases movement speed and dexterity.  At higher ranks grants additional attacks.', 0, 0, 0, 0),
 (173, 46, 35, 3, 1, 'Absorb Energy', 1, 'Absorbs a percentage of damage that the caster would take, from all sources.', 20, 2, 0, 0),
 (174, 43, 36, 3, 1, 'Force Body', 1, 'Converts a percentage of the casters current HP into FP.', 0, 0, 0, 0),
 (175, 45, 37, 3, 0, 'Mind Shield', 1, 'Protects the target from mind affecting powers and abilities.', 20, 2, 0, 1),
 (176, 45, 38, 3, 1, 'Rage', 1, 'Increases STR and CON at the cost of AC and HP damage each round.  At higher ranks grants additional attacks, that do not stack with Force Speed.', 10, 2, 0, 2),
 (177, 42, 39, 3, 0, 'Force Persuade', 1, 'Applies Domination effect to humanoid creatures with lower WIS than the caster.', 0, 0, 0, 1),
 (178, 42, 40, 3, 0, 'Confusion', 1, 'Applies Confusion effect to organic creatures with lower WIS than the caster.', 0, 0, 0, 1),
 (179, 42, 41, 3, 0, 'Force Stun', 1, 'Tranquilises an enemy or slows their reaction time.', 10, 1, 0, 1),
 (180, 42, 42, 3, 0, 'Sith Alchemy', 1, 'The power to transform living (or recently-living) flesh.', 0, 0, 0, 2),
 (181, 41, 43, 3, 0, 'Throw Saber', 1, 'The caster throws their lightsaber at nearby enemies and pulls it back to their hand.', 10, 1, 0, 0),
 (182, 51, 44, 3, 1, 'Premonition', 1, 'The caster sees a short way into the future, allowing them to avoid an untimely fate.', 0, 0, 0, 0),
 (183, 51, 45, 3, 1, 'Comprehend Speech', 1, 'The caster improves their ability to understand other languages.', 0, 0, 0, 0),
 (184, 51, 46, 3, 1, 'Force Detection', 1, 'The caster senses nearby hidden creatures.', 0, 0, 0, 0),
 (185, 46, 47, 3, 1, 'Farseeing', 1,  'The caster gets a vision of another character.', 0, 0, 0, 0),
 (186, 46, 48, 3, 1, 'Battle Meditation', 1, 'The caster boosts their nearby allies at the expense of their own abilities.', 10, 2, 0, 0),
 (187, 51, 49, 3, 0, 'Animal Bond', 1, 'The caster convinces a creature to travel and fight with them.', 0, 0, 0, 0)
 ;

-- Update existing perks.
UPDATE dbo.Perk SET PerkCategoryID=42, ForceBalanceTypeID=2 WHERE ID IN (78, 4); -- Drain Life & Force Lightning
UPDATE dbo.Perk SET PerkCategoryID=49, ForceBalanceTypeID=1 WHERE ID=19; -- Force Push
UPDATE dbo.Perk SET PerkCategoryID=42, ForceBalanceTypeID=0 WHERE ID=3; -- Force Breach
UPDATE dbo.Perk SET PerkCategoryID=45, ForceBalanceTypeID=0 WHERE ID=5; -- Force Heal
UPDATE dbo.Perk SET IsActive=0 WHERE ID IN (13, 126, 76); -- Force Spread, Chainspell, Force Aura

-- Levels for each perk.
ALTER TABLE dbo.PerkLevel
ADD SpecializationID INT NOT NULL DEFAULT 0
CONSTRAINT FK_PerkLevel_SpecializationID FOREIGN KEY REFERENCES dbo.Specialization(ID);

ALTER TABLE dbo.PerkLevel ADD BaseFPCost INT NOT NULL DEFAULT 0;

-- Update existing perk levels
UPDATE dbo.PerkLevel SET BaseFPCost=1, SpecializationID=2 WHERE ID=1774;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=2, SpecializationID=2 WHERE ID=1775;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=3, SpecializationID=2 WHERE ID=1776;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=4, SpecializationID=2 WHERE ID=1777;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=5, SpecializationID=2 WHERE ID=1778;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=6, SpecializationID=2 WHERE ID=1779;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=7, SpecializationID=2 WHERE ID=1780;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=8, SpecializationID=2 WHERE ID=1781;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=9, SpecializationID=2 WHERE ID=1782;  -- Force Breach
UPDATE dbo.PerkLevel SET BaseFPCost=10, SpecializationID=2 WHERE ID=1783;  -- Force Breach

UPDATE dbo.PerkLevel SET BaseFPCost=1, SpecializationID=2 WHERE ID=1784;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=2, SpecializationID=2 WHERE ID=1785;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=3, SpecializationID=2 WHERE ID=1786;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=4, SpecializationID=2 WHERE ID=1787;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=5, SpecializationID=2 WHERE ID=1788;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=6, SpecializationID=2 WHERE ID=1789;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=7, SpecializationID=2 WHERE ID=1790;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=8, SpecializationID=2 WHERE ID=1791;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=9, SpecializationID=2 WHERE ID=1792;  -- Force Lightning
UPDATE dbo.PerkLevel SET BaseFPCost=10, SpecializationID=2 WHERE ID=1793;  -- Force Lightning

UPDATE dbo.PerkLevel SET BaseFPCost=1, SpecializationID=0 WHERE ID=1794;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=2, SpecializationID=0 WHERE ID=1795;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=3, SpecializationID=0 WHERE ID=1796;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=4, SpecializationID=0 WHERE ID=1797;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=5, SpecializationID=0 WHERE ID=1798;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=6, SpecializationID=2 WHERE ID=1799;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=7, SpecializationID=2 WHERE ID=1800;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=8, SpecializationID=2 WHERE ID=1801;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=9, SpecializationID=2 WHERE ID=1802;  -- Force Heal
UPDATE dbo.PerkLevel SET BaseFPCost=10, SpecializationID=2 WHERE ID=1803;  -- Force Heal

UPDATE dbo.PerkLevel SET BaseFPCost=4, SpecializationID=0 WHERE ID=1901;  -- Force Push
UPDATE dbo.PerkLevel SET BaseFPCost=6, SpecializationID=0 WHERE ID=1902;  -- Force Push
UPDATE dbo.PerkLevel SET BaseFPCost=8, SpecializationID=3 WHERE ID=1903;  -- Force Push
UPDATE dbo.PerkLevel SET BaseFPCost=10, SpecializationID=3 WHERE ID=1904;  -- Force Push
UPDATE dbo.PerkLevel SET BaseFPCost=12, SpecializationID=3 WHERE ID=1905;  -- Force Push

UPDATE dbo.PerkLevel SET BaseFPCost=2, SpecializationID=2 WHERE ID=1972;  -- Drain Life
UPDATE dbo.PerkLevel SET BaseFPCost=4, SpecializationID=2 WHERE ID=1973;  -- Drain Life
UPDATE dbo.PerkLevel SET BaseFPCost=6, SpecializationID=2 WHERE ID=1974;  -- Drain Life
UPDATE dbo.PerkLevel SET BaseFPCost=8, SpecializationID=2 WHERE ID=1975;  -- Drain Life
UPDATE dbo.PerkLevel SET BaseFPCost=10, SpecializationID=2 WHERE ID=1976;  -- Drain Life

DECLARE @PerkLevelID INT;

 INSERT INTO dbo.PerkLevel (PerkID, Level, Price, Description, SpecializationID, BaseFPCost) VALUES
 (172,1,2,'Increases movement speed by 10% and Dexterity by 2.', 0, 2),
 (172,2,2,'Increases movement speed by 20% and Dexterity by 4.', 0, 4),
 (172,3,3,'Increases movement speed by 30%, Dexterity by 6 and grants an extra attack.', 0, 6),
 (172,4,3,'Increases movement speed by 40%, Dexterity by 8 and grants an extra attack.', 1, 8),
 (172,5,12,'Increases movement speed by 50%, Dexterity by 10 and grants two extra attacks and Epic Dodge.', 1, 20), 
 (173,1,2,'Grants 10% immunity to all damage while the caster retains concentration.', 0, 2),
 (173,2,2,'Grants 20% immunity to all damage while the caster retains concentration.', 0, 4),
 (173,3,3,'Grants 30% immunity to all damage while the caster retains concentration.', 0, 6),
 (173,4,3,'Grants 40% immunity to all damage while the caster retains concentration.', 2, 8),
 (173,5,4,'Grants 50% immunity to all damage while the caster retains concentration.', 2, 10),
 (174,1,3,'Converts 10% of the casters current HP into FP.', 0, 0),
 (174,2,4,'Converts 20% of the casters current HP into FP.', 0, 0),
 (174,3,5,'Converts 35% of the casters current HP into FP.', 0, 0),
 (174,4,6,'Converts 50% of the casters current HP into FP.', 0, 0),
 (175,1,4,'Immune to Tranquilisation effects while concentrating.', 0, 1),
 (175,2,6,'Immune to Tranquilisation, Confusion and Persuade effects while concentrating.', 0, 2),
 (175,3,8,'Immune to Tranquilisation, Confusion, Persuade and Drain effects while concentrating.', 1, 3),
 (176,1,2,'Increases Strength and Dexterity by 2 while concentrating but reduces AC by 2 and deals 2 damage per round.', 0, 2),
 (176,2,2,'Increases Strength and Dexterity by 4 while concentrating but reduces AC by 2 and deals 4 damage per round.', 0, 4),
 (176,3,5,'Increases Strength and Dexterity by 6 while concentrating and grants an extra attack but reduces AC by 4 and deals 6 damage per round.', 0, 6),
 (176,4,5,'Increases Strength and Dexterity by 8 while concentrating and grants an extra attack but reduces AC by 4 and deals 8 damage per round.', 1, 8),
 (176,5,8,'Increases Strength and Dexterity by 10 while concentrating and grants two extra attacks but reduces AC by 2 and deals 10 damage per round.', 1, 10),
 (177,1,7,'Applies Domination effect to a single humanoid target with lower WIS than the caster, while the caster concentrates.', 0, 8),
 (177,2,7,'Applies Domination effect to all hostile humanoid targets within 5m with lower WIS than the caster, while the caster concentrates.', 2, 20),
 (178,1,7,'Applies Confusion effect to a single non-mechanical target with lower WIS than the caster, while the caster concentrates.', 0, 8),
 (178,2,7,'Applies Confusion effect to all hostile non-mechanical targets within 10m with lower WIS than the caster, while the caster concentrates.', 2, 20),
 (179,1,4,'Single target is Tranquilised while the caster concentrates or, if resisted, gets -5 to AB and AC.', 0, 8),
 (179,2,7,'Target and nearest other enemy within 10m is Tranquilised while the caster concentrates or, if resisted, get -5 to AB and AC.', 2, 12),
 (179,3,10,'Target and all other enemies within 10 are Tranquilised while the caster concentrates or, if resisted, get -5 to AB and AC.', 2, 20),
 (180,1,0,'Unlocks Sith Alchemy.', 2, 0),
 (180,2,7,'When used on a corpse, raises the creature as a henchman while the caster concentrates.', 2, 25),
 (180,3,7,'Alchemist can create monsters.', 2, 300),
 (180,4,0,'Alchemist can employ monsters as henchmen while they concentrate.', 2, 5),
 (181,1,2,'Throw your equipped lightsaber up to 5m for (saber damage + INT modifier) * 100%.  This ability hits automatically.', 0, 4),
 (181,2,2,'Throw your equipped lightsaber up to 10m for (saber damage + INT modifier) * 125%.  This ability hits automatically.', 0, 5),
 (181,3,2,'Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 160%.  This ability hits automatically.', 0, 6),
 (181,4,2,'Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 200%.  This ability hits automatically and will chain to a second target within 5m of the first.', 1, 8),
 (181,5,2,'Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 250%.  This ability hits automatically and will chain to a second and third target within 5m each.', 1, 10),
 (182,1,4,'The next time the caster would die in the next 30 minutes, they are instead healed to 25% of their max HP.', 0, 5),
 (182,2,7,'The next time the caster would die in the next 30 minutes, they are instead healed to 50% of their max HP.', 0, 5),
 (182,3,10,'For 12s after casting, the caster is immune to all damage, and the next time the caster would die in the next 30 minutes, they are instead healed to 25% of their max HP.', 3, 16),
 (183,1,3,'The caster counts has having 5 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.', 0, 1),
 (183,2,4,'The caster counts has having 10 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.', 0, 2),
 (183,3,5,'The caster counts has having 15 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.', 3, 3),
 (183,4,6,'The caster counts has having 20 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.', 3, 4),
 (184,1,2,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 0, 1),
 (184,2,2,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 0, 2),
 (184,3,3,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 3, 3),
 (184,4,3,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 3, 4),
 (184,5,4,'The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).', 3, 5),
 (185,1,10,'The caster attempts to view what another character is currently doing.', 0, 30),
 (186,1,4, 'The caster gets -5 AB & AC but their nearby party members get +5 AB & AC', 0, 1),
 (186,2,6, 'The caster gets -10 AB & AC but their nearby party members get +10 AB & AC', 0, 2),
 (186,3,8, 'The caster and nearby enemies get -10 AB & AC but the nearby party members get +10 AB & AC', 0, 3),
 (187,1,2, 'The caster befriends an animal or beast with up to Challenge Rating 4.', 0, 4),
 (187,2,2, 'The caster befriends an animal or beast with up to Challenge Rating 8.', 0, 8),
 (187,3,3, 'The caster befriends an animal or beast with up to Challenge Rating 12.', 0, 12),
 (187,4,3, 'The caster befriends an animal or beast with up to Challenge Rating 16.', 3, 16),
 (187,5,4, 'The caster befriends an animal or beast with up to Challenge Rating 20.', 3, 20),
 (187,6,5, 'The caster befriends an animal or beast with any Challenge Rating.', 3, 30)
 ;
 
SET @PerkLevelID = SCOPE_IDENTITY();

-- Add the skill requirement for each perk level.
 INSERT INTO dbo.PerkLevelSkillRequirement (PerkLevelID, SkillID, RequiredRank) VALUES
  (@PerkLevelID-59,20,0),
  (@PerkLevelID-58,20,10),
  (@PerkLevelID-57,20,25),
  (@PerkLevelID-56,20,40),
  (@PerkLevelID-55,20,80),
  (@PerkLevelID-54,20,0),
  (@PerkLevelID-53,20,15),
  (@PerkLevelID-52,20,30),
  (@PerkLevelID-51,20,45),
  (@PerkLevelID-50,20,60),
  (@PerkLevelID-49,20,10),
  (@PerkLevelID-48,20,25),
  (@PerkLevelID-47,20,40),
  (@PerkLevelID-46,20,55),
  (@PerkLevelID-45,20,10),
  (@PerkLevelID-44,20,30),
  (@PerkLevelID-43,20,50),
  (@PerkLevelID-42,20,10),
  (@PerkLevelID-41,20,30),
  (@PerkLevelID-40,20,50),
  (@PerkLevelID-39,20,70),
  (@PerkLevelID-38,20,90),
  (@PerkLevelID-37,19,40),
  (@PerkLevelID-36,19,80),
  (@PerkLevelID-35,19,40),
  (@PerkLevelID-34,19,80),
  (@PerkLevelID-33,19,10),
  (@PerkLevelID-32,19,50),
  (@PerkLevelID-31,19,80),
  (@PerkLevelID-30,19,0),
  (@PerkLevelID-29,19,80),
  (@PerkLevelID-28,19,90),
  (@PerkLevelID-27,19,90),
  (@PerkLevelID-26,19,10),
  (@PerkLevelID-25,19,20),
  (@PerkLevelID-24,19,30),
  (@PerkLevelID-23,19,40),
  (@PerkLevelID-22,19,50),
  (@PerkLevelID-21,21,0),
  (@PerkLevelID-20,21,20),
  (@PerkLevelID-19,21,50),
  (@PerkLevelID-18,21,0),
  (@PerkLevelID-17,21,15),
  (@PerkLevelID-16,21,30),
  (@PerkLevelID-15,21,45),
  (@PerkLevelID-14,21,0),
  (@PerkLevelID-13,21,5),
  (@PerkLevelID-12,21,20),
  (@PerkLevelID-11,21,35),
  (@PerkLevelID-10,21,50),
  (@PerkLevelID-9,21,80),
  (@PerkLevelID-8,21,40),
  (@PerkLevelID-7,21,60),
  (@PerkLevelID-6,21,80),
  (@PerkLevelID-5,21,10),
  (@PerkLevelID-4,21,25),
  (@PerkLevelID-3,21,40),
  (@PerkLevelID-2,21,55),
  (@PerkLevelID-1,21,70),
  (@PerkLevelID, 21, 85)
  ;
 
-- Add a DM-granted quest for Sith Alchemy and put its ID in here in place of 99.
INSERT INTO dbo.Quest (ID, Name, JournalTag, FameRegionID, RequiredFameAmount, AllowRewardSelection, RewardGold, RewardFame, IsRepeatable) VALUES
 (99, 'Sith Alchemy', '', 0, 0, 0, 0, 0, 0);

-- Set the Sith Alchemy perk level 1 to be gated on the above quest.
INSERT INTO dbo.PerkLevelQuestRequirement (PerkLevelID, RequiredQuestID) VALUES (@PerkLevelID-30, 99);