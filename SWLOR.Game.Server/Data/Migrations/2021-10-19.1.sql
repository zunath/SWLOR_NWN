-- Lightfoil Fixes
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 221 AND BaseLevel = 1;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 611 AND BaseLevel = 1;
UPDATE CraftBlueprint Set SkillID = 22 Where PerkID = 151;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 211;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 216;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 612;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 617;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 622;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 627;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 632;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where ID = 637;
UPDATE CraftBlueprint Set EnhancementSlots = 1 Where ID = 212;
UPDATE CraftBlueprint Set EnhancementSlots = 1 Where ID = 217;
UPDATE CraftBlueprint Set EnhancementSlots = 1 Where ID = 613;
UPDATE CraftBlueprint Set EnhancementSlots = 1 Where ID = 618;
UPDATE CraftBlueprint Set EnhancementSlots = 1 Where ID = 623;
UPDATE CraftBlueprint Set EnhancementSlots = 1 Where ID = 628;
UPDATE CraftBlueprint Set EnhancementSlots = 1 Where ID = 633;
UPDATE CraftBlueprint Set EnhancementSlots = 1 Where ID = 638;
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ID = 213; 
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ID = 218; 
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ID = 614;
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ID = 619;
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ID = 624;
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ID = 629;
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ID = 634;
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ID = 639;
UPDATE CraftBlueprint Set EnhancementSlots = 3 Where ID = 214; 
UPDATE CraftBlueprint Set EnhancementSlots = 3 Where ID = 219;
UPDATE CraftBlueprint Set EnhancementSlots = 3 Where ID = 615;
UPDATE CraftBlueprint Set EnhancementSlots = 3 Where ID = 620;
UPDATE CraftBlueprint Set EnhancementSlots = 3 Where ID = 625;
UPDATE CraftBlueprint Set EnhancementSlots = 3 Where ID = 630;
UPDATE CraftBlueprint Set EnhancementSlots = 3 Where ID = 635;
UPDATE CraftBlueprint Set EnhancementSlots = 3 Where ID = 640;
UPDATE CraftBlueprint Set EnhancementSlots = 4 Where ID = 215; 
UPDATE CraftBlueprint Set EnhancementSlots = 4 Where ID = 220; 
UPDATE CraftBlueprint Set EnhancementSlots = 4 Where ID = 616; 
UPDATE CraftBlueprint Set EnhancementSlots = 4 Where ID = 621; 
UPDATE CraftBlueprint Set EnhancementSlots = 4 Where ID = 626; 
UPDATE CraftBlueprint Set EnhancementSlots = 4 Where ID = 631; 
UPDATE CraftBlueprint Set EnhancementSlots = 4 Where ID = 636; 
UPDATE CraftBlueprint Set EnhancementSlots = 4 Where ID = 641;

UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2071 AND RequiredRank = 5;
UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2072 AND RequiredRank = 10;
UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2073 AND RequiredRank = 15;
UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2074 AND RequiredRank = 20;
UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2075 AND RequiredRank = 25;
UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2076 AND RequiredRank = 30;
UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2077 AND RequiredRank = 35;
UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2078 AND RequiredRank = 40;
UPDATE PerkLevelSkillRequirement Set SkillID = 22 WHERE PerkLevelID = 2079 AND RequiredRank = 45;

-- Cluster Requirement Decrease
UPDATE CraftBlueprint Set MainMinimum = 2 Where ItemResref = 'c_cluster_red';
UPDATE CraftBlueprint Set MainMinimum = 2 Where ItemResref = 'c_cluster_yellow';
UPDATE CraftBlueprint Set MainMinimum = 2 Where ItemResref = 'c_cluster_blue';
UPDATE CraftBlueprint Set MainMinimum = 2 Where ItemResref = 'c_cluster_green' ;

-- Set all furniture to have 2 enhancement slots (This is a universal increase to them all!)
UPDATE CraftBlueprint Set EnhancementSlots = 2 Where ItemResref = 'furniture' ;

-- "Fix" Medical Enhancement Levels
UPDATE CraftBlueprint Set BaseLevel = 5 Where SkillID = 17 AND ID = 533;
UPDATE CraftBlueprint Set BaseLevel = 10 Where SkillID = 17 AND ID = 534;
UPDATE CraftBlueprint Set BaseLevel = 15 Where SkillID = 17 AND ID = 535;
UPDATE CraftBlueprint Set BaseLevel = 20 Where SkillID = 17 AND ID = 536;
UPDATE CraftBlueprint Set BaseLevel = 25 Where SkillID = 17 AND ID = 537;

UPDATE CraftBlueprint Set EnhancementSlots = 0 Where SkillID = 17 AND ID = 533;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where SkillID = 17 AND ID = 534;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where SkillID = 17 AND ID = 535;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where SkillID = 17 AND ID = 536;
UPDATE CraftBlueprint Set EnhancementSlots = 0 Where SkillID = 17 AND ID = 537;



-- Armorsmithing Core Parts Balance
UPDATE CraftBlueprint Set MainMinimum = 2 Where SkillID = 13 AND ID = 224;
UPDATE CraftBlueprint Set MainMinimum = 2 Where SkillID = 13 AND ID = 22;

UPDATE CraftBlueprint Set BaseLevel = 0 Where SkillID = 13 AND ID = 223;
UPDATE CraftBlueprint Set BaseLevel = 0 Where SkillID = 13 AND ID = 224;
UPDATE CraftBlueprint Set BaseLevel = 0 Where SkillID = 13 AND ID = 225;

UPDATE CraftBlueprint Set BaseLevel = 1 Where SkillID = 13 AND ID = 227;
UPDATE CraftBlueprint Set BaseLevel = 1 Where SkillID = 13 AND ID = 228;
UPDATE CraftBlueprint Set BaseLevel = 1 Where SkillID = 13 AND ID = 229;

-- Add Melee Tuskens
INSERT INTO swlor.SpawnObject (ID, SpawnID, Resref, Weight, SpawnRule, NPCGroupID, BehaviourScript, DeathVFXID, AIFlags)
VALUES (111, 46, 'tusken_melee', 50,'', 27, 'StandardBehaviour', 0, 7);

-- Add Sand Worm
INSERT INTO Spawn (ID, Name, SpawnObjectTypeID)
VALUES(48, 'Tatooine SandWorm', 1);

INSERT INTO NPCGroup (ID, Name)
VALUES(31,'Sandworm');

INSERT INTO LootTable (ID, Name)
VALUES(118,'Tatooine Sand Worm');

INSERT INTO SpawnObject (ID, SpawnID, Resref, Weight, SpawnRule, NPCGroupID, BehaviourScript, DeathVFXID, AIFlags)
VALUES (112, 48, 'sandworm', 50,'', 31, 'DarkForceUser', 0, 7);

INSERT INTO LootTableItem(ID, LootTableID, Resref, MaxQuantity, Weight, IsActive, SpawnRule)
VALUES(1824, 118,'ass_power', 1, 25, 1, '');
