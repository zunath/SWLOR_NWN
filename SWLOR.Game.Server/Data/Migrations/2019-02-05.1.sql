-- Add crafting recipes for starship enhancements.
INSERT INTO dbo.CraftBlueprint (ID, CraftCategoryID, BaseLevel, ItemName, ItemResref, Quantity, SkillID, CraftDeviceID, PerkID, RequiredPerkLevel, IsActive, MainComponentTypeID, MainMinimum, MainMaximum, SecondaryComponentTypeID, SecondaryMinimum, SecondaryMaximum, TertiaryComponentTypeID, TertiaryMinimum, TertiaryMaximum, EnhancementSlots, BaseStructureID) VALUES
  (666, 46, 25, 'Starship Auxiliary Blaster', 'sswpn1', 1, 22, 4, 96, 5, 1, 19, 2, 4, 32, 2, 4, 0, 0, 0, 4, NULL),
  (667, 46, 46, 'Starship Auxiliary Light Cannon', 'sswpn2', 1, 22, 4, 96, 7, 1, 19, 4, 8, 32, 4, 8, 0, 0, 0, 4, NULL),
  (668, 46, 23, 'Auxiliary Shield Generator (Small)', 'ssshld1', 1, 22, 4, 96, 5, 1, 15, 4, 6, 45, 2, 2, 0, 0, 0, 4, NULL),
  (669, 46, 45, 'Auxiliary Shield Generator (Medium)', 'ssshld2', 1, 22, 4, 96, 7, 1, 15, 4, 6, 45, 2, 2, 0, 0, 0, 4, NULL),
  (670, 46, 20, 'Auxiliary Thruster (Small)', 'ssspd1', 1, 22, 4, 96, 5, 1, 32, 3, 4, 45, 2, 2, 0, 0, 0, 4, NULL),
  (671, 46, 38, 'Auxiliary Thruster (Medium)', 'ssspd2', 1, 22, 4, 96, 7, 1, 32, 5, 8, 45, 4, 4, 0, 0, 0, 4, NULL),
  (672, 46, 25, 'Auxiliary Targeter (Basic)', 'ssrang1', 1, 22, 4, 96, 5, 1, 42, 3, 4, 0, 0, 0, 0, 0, 0, 4, NULL),
  (673, 46, 42, 'Auxiliary Targeter (Improved)', 'ssrang2', 1, 22, 4, 96, 7, 1, 42, 5, 8, 0, 0, 0, 0, 0, 0, 4, NULL),
  (674, 46, 20, 'Additional Fuel Tank (Small)', 'ssfuel1', 1, 13, 1, 124, 5, 1, 2, 2, 4, 39, 1, 2, 0, 0, 0, 4, NULL),
  (675, 46, 40, 'Additional Fuel Tank (Medium)', 'ssfuel2', 1, 13, 1, 124, 7, 1, 2, 2, 4, 40, 1, 2, 0, 0, 0, 4, NULL),
  (676, 46, 20, 'Additional Stronidium Tank (Small)', 'ssstron1', 1, 13, 1, 124, 5, 1, 43, 1, 2, 39, 1, 2, 0, 0, 0, 4, NULL),
  (677, 46, 40, 'Additional Stronidium Tank (Medium)', 'ssstron2', 1, 13, 1, 124, 7, 1, 43, 2, 4, 40, 1, 2, 0, 0, 0, 4, NULL),
  (678, 46, 20, 'Cloaking Generator (Small)', 'ssstlth1', 1, 22, 4, 96, 5, 1, 15, 3, 4, 45, 2, 2, 0, 0, 0, 4, NULL),
  (679, 46, 38, 'Cloaking Generator (Medium)', 'ssstlth2', 1, 22, 4, 96, 7, 1, 15, 5, 8, 45, 4, 4, 0, 0, 0, 4, NULL),
  (680, 46, 24, 'Scanning Array (Small)', 'ssscan1', 1, 22, 4, 96, 5, 1, 15, 3, 4, 42, 2, 2, 0, 0, 0, 4, NULL),
  (681, 46, 40, 'Scanning Array (Medium)', 'ssscan2', 1, 22, 4, 96, 7, 1, 15, 5, 8, 42, 4, 4, 0, 0, 0, 4, NULL);

-- Add perks for the Piloting skill.
-- New category for Piloting perks.
INSERT INTO dbo.PerkCategory (ID, Name, IsActive, Sequence) VALUES
 (37, 'Piloting', 1, 33); 
-- Definitions for each perk
INSERT INTO dbo.Perk (ID, PerkCategoryID, Name, IsActive, ScriptName, Description, EnmityAdjustmentRuleID, ExecutionTypeID) VALUES
 (161,37,'Racer', 1, 'Piloting.Racer', 'Increases movement speed when piloting a starship or speeder by 10% per rank.',0,0),
 (162,37,'Evasive', 1, 'Piloting.Evasive', 'Increases effective piloting skill by 3 per rank when dodging enemy attacks.',0,0),
 (163,37,'Hunter', 1, 'Piloting.Hunter', 'Increases the relative chance of finding hostile ships when determining space encounters.',0,0),
 (164,37,'Sneak', 1, 'Piloting.Sneak', 'Decreases the relative chance of finding hostile ships when determining space encounters.',0,0),
 (165,37,'Scavenger', 1, 'Piloting.Scavenger', 'Increases the relative chance of finding salvage when determining space encounters.',0,0),
 (166,37,'Sniper', 1, 'Piloting.Sniper', 'When acting as a gunner, increases the range of the ship.',0,0),
 (167,37,'Crack Shot', 1, 'Piloting.CrackShot', 'When acting as a gunner, increases effective piloting skill by 3 when making attacks.',0,0),
 (168,37,'Systems Optimization', 1, 'Piloting.SystemsOptimization', 'When in a starship but not piloting or crewing the guns, reduces Stronidium consumption by 1 per rank on each attack and defense (to a min of 1).',0,0),
 (169,37,'Combat Repair', 1, 'Piloting.CombatRepair', 'Allows repairing starships in space.  At level 2, halves the repair time.',0,0),
 (170,37,'Careful Pilot', 1, 'Piloting.CarefulPilot', 'You may re-roll a failed Piloting check to take off, land, or jump through hyperspace, conserving fuel.',0,0);
-- Levels for each perk.

DECLARE @PerkLevelID INT;

 INSERT INTO dbo.PerkLevel (PerkID, Level, Price, Description) VALUES
 (161,1,2,'Increases movement speed by 10%.'),
 (161,2,2,'Increases movement speed by 20%.'),
 (161,3,3,'Increases movement speed by 30%.'),
 (161,4,3,'Increases movement speed by 40%.'),
 (161,5,4,'Increases movement speed by 50%.'), 
 (162,1,2,'Increases piloting by 3 when evading.'),
 (162,2,2,'Increases piloting by 6 when evading.'),
 (162,3,3,'Increases piloting by 9 when evading.'),
 (162,4,3,'Increases piloting by 12 when evading.'),
 (162,5,4,'Increases piloting by 15 when evading.'),
 (163,1,2,'Increases relative chance of hostile encounters by 1 each.'),
 (163,2,2,'Increases relative chance of hostile encounters by 2 each.'),
 (163,3,3,'Increases relative chance of hostile encounters by 3 each.'),
 (163,4,3,'Increases relative chance of hostile encounters by 4 each.'),
 (163,5,4,'Increases relative chance of hostile encounters by 5 each.'),
 (164,1,2,'Decreases relative chance of hostile encounters by 1 each.'),
 (164,2,2,'Decreases relative chance of hostile encounters by 2 each.'),
 (164,3,3,'Decreases relative chance of hostile encounters by 3 each.'),
 (164,4,3,'Decreases relative chance of hostile encounters by 4 each.'),
 (164,5,4,'Decreases relative chance of hostile encounters by 5 each.'),
 (165,1,2,'Increases relative chance of salvage encounters by 1 each.'), 
 (165,2,2,'Increases relative chance of salvage encounters by 2 each.'),
 (165,3,3,'Increases relative chance of salvage encounters by 3 each.'),
 (165,4,3,'Increases relative chance of salvage encounters by 4 each.'),
 (165,5,4,'Increases relative chance of salvage encounters by 5 each.'),
 (166,1,2,'Increases range of the ship by 1.'),
 (166,2,2,'Increases range of the ship by 2.'),
 (166,3,3,'Increases range of the ship by 3.'),
 (166,4,3,'Increases range of the ship by 4.'),
 (166,5,4,'Increases range of the ship by 5.'),
 (167,1,2,'Increases piloting by 3 when crewing ship guns.'),
 (167,2,2,'Increases piloting by 6 when crewing ship guns.'),
 (167,3,3,'Increases piloting by 9 when crewing ship guns.'),
 (167,4,3,'Increases piloting by 12 when crewing ship guns.'),
 (167,5,4,'Increases piloting by 15 when crewing ship guns.'),
 (168,1,3,'Reduces Stronidium usage by 1 (min 1).'),
 (168,2,4,'Reduces Stronidium usage by 2 (min 1).'),
 (168,3,5,'Reduces Stronidium usage by 3 (min 1).'),
 (168,4,6,'Reduces Stronidium usage by 4 (min 1).'),
 (168,5,7,'Reduces Stronidium usage by 5 (min 1).'),
 (169,1,5,'Allows repairing ships in space.'),
 (169,2,2,'Halves the time needed to repair ships.'),
 (170,1,4,'Grants a re-roll on failure to take off, land, or jump through hyperspace.');
 
SET @PerkLevelID = SCOPE_IDENTITY();

-- Add the piloting skill requirement for each perk level.
 INSERT INTO dbo.PerkLevelSkillRequirement (PerkLevelID, SkillID, RequiredRank) VALUES
  (@PerkLevelID-42,36,5),
  (@PerkLevelID-41,36,10),
  (@PerkLevelID-40,36,15),
  (@PerkLevelID-39,36,20),
  (@PerkLevelID-38,36,25),
  (@PerkLevelID-37,36,10),
  (@PerkLevelID-36,36,20),
  (@PerkLevelID-35,36,30),
  (@PerkLevelID-34,36,40),
  (@PerkLevelID-33,36,50),
  (@PerkLevelID-32,36,20),
  (@PerkLevelID-31,36,35),
  (@PerkLevelID-30,36,50),
  (@PerkLevelID-29,36,65),
  (@PerkLevelID-28,36,80),
  (@PerkLevelID-27,36,15),
  (@PerkLevelID-26,36,30),
  (@PerkLevelID-25,36,45),
  (@PerkLevelID-24,36,60),
  (@PerkLevelID-23,36,75),
  (@PerkLevelID-22,36,25),
  (@PerkLevelID-21,36,40),
  (@PerkLevelID-20,36,55),
  (@PerkLevelID-19,36,70),
  (@PerkLevelID-18,36,85),
  (@PerkLevelID-17,36,50),
  (@PerkLevelID-16,36,60),
  (@PerkLevelID-15,36,70),
  (@PerkLevelID-14,36,80),
  (@PerkLevelID-13,36,90),
  (@PerkLevelID-12,36,8),
  (@PerkLevelID-11,36,18),
  (@PerkLevelID-10,36,28),
  (@PerkLevelID-9,36,38),
  (@PerkLevelID-8,36,48),
  (@PerkLevelID-7,36,50),
  (@PerkLevelID-6,36,60),
  (@PerkLevelID-5,36,70),
  (@PerkLevelID-4,36,80),
  (@PerkLevelID-3,36,90),
  (@PerkLevelID-2,36,15),
  (@PerkLevelID-1,36,20),
  (@PerkLevelID, 36, 20);

-- Add loot for starships
INSERT INTO dbo.LootTable (ID, Name) VALUES
 (52, 'Starship Pirates - Basic'),
 (53, 'Starship Pirates - Rare');

INSERT INTO dbo.LootTableItem (LootTableID, Resref, MaxQuantity, Weight, IsActive) VALUES
 (52, 'stronidium', 20, 20, 1),
 (52, 'fuel_cell', 5, 20, 1),
 (53, 'sswpn1', 1, 10, 1),
 (53, 'ssshld1', 1, 10, 1),
 (53, 'ssspd1', 1, 10, 1),
 (53, 'ssrang1', 1, 10, 1),
 (53, 'ssfuel1', 1, 10, 1),
 (53, 'ssstron1', 1, 10, 1),
 (53, 'ssstlth1', 1, 10, 1),
 (53, 'ssscan1', 1, 10, 1),
 (53, 'sswpn2', 1, 1, 1),
 (53, 'ssshld2', 1, 1, 1),
 (53, 'ssspd2', 1, 1, 1),
 (53, 'ssrang2', 1, 1, 1),
 (53, 'ssfuel2', 1, 1, 1),
 (53, 'ssstron2', 1, 1, 1),
 (53, 'ssstlth2', 1, 1, 1),
 (53, 'ssscan2', 1, 1, 1);

 UPDATE dbo.SpaceEncounter SET LootTable=52 WHERE Type in (1,4);
 UPDATE dbo.SpaceEncounter SET LootTable=53 WHERE ID in (2,7);