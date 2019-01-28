-- Space system updates

-- New base structure type - starship
INSERT INTO dbo.BaseStructureType (ID, Name, IsActive, CanPlaceInside, CanPlaceOutside) VALUES (13, 'Starship', 1, 0, 1);

-- New base structures - Spaceship Dock, Light Freighter, Light Escort
INSERT INTO dbo.BaseStructure (ID, BaseStructureTypeID, Name, PlaceableResref, ItemResref, IsActive, Power, CPU, Durability, Storage, HasAtmosphere, ReinforcedStorage, RequiresBasePower, ResourceStorage, RetrievalRating, FuelRating, DefaultStructureModeID) VALUES
  (179, 7, 'Starship Dock', 'starship_dock', 'silo', 1, 30, 5, 15, 0, 0, 0, 1, 0, 0, 0, 0),
  (180, 13, 'Starship: Light Freighter 1', 'starship1', 'starship', 1, 0, 0, 50, 400, 0, 100, 0, 30, 0, 0, 0),
  (181, 13, 'Starship: Light Escort 1', 'starship2', 'starship', 1, 0, 0, 50, 400, 0, 300, 0, 10, 0, 0, 0);

-- New building type (4 = Starship Interior)
INSERT INTO dbo.BuildingType (ID, Name) VALUES (4, 'Starship');

-- New buildings - Spaceship 1 (Light Freighter), Spaceship 2 (Light Escort), interior & exterior
INSERT INTO dbo.BuildingStyle (ID, Name, Resref, BaseStructureID, IsDefault, DoorRule, IsActive, BuildingTypeID, PurchasePrice, DailyUpkeep, FurnitureLimit) VALUES
  (20, 'Light Freighter 1', 'starship1', 180, 1, 'Starship1Rule', 1, 1, 0, 0, 0), 
  (21, 'Light Escort 1', 'starship2', 181, 1, 'Starship2Rule', 1, 1, 0, 0, 0),
  (22, 'Light Freighter 1 Interior', 'starship1_int', 180, 1, '', 1, 4, 0, 0, 60),  
  (23, 'Light Escort 1 Interior', 'starship2_int', 181, 1, '', 1, 4, 0, 0, 45);

-- New recipes - Hyperdrive, Starship Hull Plates, Starship Weapon, Starship Dock, Starship 1 (Light Transport), Starship 2 (Light Escort)
INSERT INTO dbo.ComponentType (ID, Name) VALUES (63, 'Hyperdrive'), (64, 'Hull Plating'), (65, 'Starship Weapon');
INSERT INTO dbo.CraftBlueprintCategory(ID, Name, IsActive) VALUES (46, 'Starship Production', 1);

-- The Base Structure ID and component information are all used as a foreign key in CraftBlueprint.  So commit the above changes, and then move on to the later ones. 
GO

INSERT INTO dbo.CraftBlueprint (ID, CraftCategoryID, BaseLevel, ItemName, ItemResref, Quantity, SkillID, CraftDeviceID, PerkID, RequiredPerkLevel, IsActive, MainComponentTypeID, MainMinimum, MainMaximum, SecondaryComponentTypeID, SecondaryMinimum, SecondaryMaximum, TertiaryComponentTypeID, TertiaryMinimum, TertiaryMaximum, EnhancementSlots, BaseStructureID) VALUES
  (659, 46, 8, 'Hyperdrive', 'hyperdrive', 1, 22, 4, 96, 7, 1, 42, 3, 3, 15, 4, 12, 45, 2, 2, 4, NULL),
  (660, 46, 6, 'Hull Plating', 'hull_plating', 1, 13, 1, 124, 7, 1, 2, 4, 12, 0, 0, 0, 0, 0, 0, 4, NULL),
  (661, 46, 7, 'Light Starship Blaster', 'ship_blaster_1', 1, 22, 4, 96, 7, 1, 19, 3, 6, 32, 4, 8, 0, 0, 0, 4, NULL),
  (662, 46, 10, 'Starship Dock', 'silo', 1, 15, 5, 2, 5, 1, 41, 1, 2, 43, 6, 12, 45, 1, 2, 4, 179),
  (663, 46, 0, 'Starship 1 (Light Transport 1)', 'starship', 1, 15, 5, 2, 7, 1, 63, 1, 1, 64, 4, 6, 65, 1, 1, 4, 180),
  (664, 46, 0, 'Starship 2 (Light Escort 1)', 'starship', 1, 15, 5, 2, 7, 1, 63, 1, 1, 64, 2, 6, 65, 4, 5, 4, 181),
  (665, 45, 30, 'Starship Repair Kit', 'ss_rep', 1, 15, 5, 2, 5, 1, 2, 3, 6, 42, 2, 4, 32, 1, 3, 0, NULL);

-- New Location and Starcharts fields for bases.
ALTER TABLE dbo.PCBase ADD ShipLocation nVarChar(64), Starcharts int;

-- Change the Sector constraint to accept "SS" (starship) as a Sector. 
alter table dbo.PCBase drop constraint CK_PCBase_Sector;
alter table dbo.PCBase WITH CHECK ADD  CONSTRAINT [CK_PCBase_Sector] CHECK  (([Sector]='SE' OR [Sector]='SW' OR [Sector]='NE' OR [Sector]='NW' OR [Sector]='AP' OR [Sector]='SS'))

-- Insert data into Loot and LootTable tables.
INSERT INTO dbo.LootTable (ID, Name) VALUES (51, 'Space - Basic Loot');
INSERT INTO dbo.LootTableItem (LootTableID, Resref, MaxQuantity, Weight, IsActive) VALUES
 ( 51, 'rruchi', 1, 3, 1),
 ( 51, 'stalluchi', 1, 3, 1),
 ( 51, 'tinnuchi', 1, 3, 1),
 ( 51, 'plexite_gem', 1, 2, 1),
 ( 51, 'ultranio', 1, 1, 1),
 ( 51, 'coonlank_yellow', 1, 1, 1),
 ( 51, 'coonlank_red', 1, 1, 1),
 ( 51, 'coonlank_blue', 1, 1, 1),
 ( 51, 'coonlank_green', 1, 1, 1),
 ( 51, 'vendusii_gem', 1, 1, 1),
 ( 51, 'hzzuntil', 1, 1, 1),
 ( 51, 'corylus', 1, 2, 1),
 ( 51, 'porlang', 1, 3, 1),
 ( 51, 'harvino', 1, 3, 1),
 ( 51, 'arvvina', 1, 2, 1),
 ( 51, 'engina', 1, 2, 1),
 ( 51, 'fabrina', 1, 2, 1),
 ( 51, 'coqina', 1, 2, 1),
 ( 51, 'weevina', 1, 2, 1),
 ( 51, 'medcina', 1, 2, 1),
 ( 51, 'uunichi', 1, 3, 1),
 ( 51, 'pawisis', 1, 5, 1),
 ( 51, 'pefoate', 1, 5, 1),
 ( 51, 'sygium_gem', 1, 1, 1),
 ( 51, 'regvis_gem', 1, 1, 1),
 ( 51, 'hollinium', 1, 3, 1),
 ( 51, 'omedia', 1, 5, 1),
 ( 51, 'nibullan', 1, 3, 1);
 
-- New Events table for space encounters
CREATE TABLE SpaceEncounter (
 ID int IDENTITY(1,1) NOT NULL,
  Planet nVarChar(32), 
  Type int,
  Chance int,
  Difficulty int,
  LootTable int);

-- Commit the table creation, so that we can load data into it below.  
 GO

-- Insert data into Events table. 
INSERT INTO dbo.SpaceEncounter (Planet, Type, Chance, Difficulty, LootTable) VALUES 
 ('Viscara', 1, 15, 10, 51), 
 ('Viscara', 1, 4, 20, 51), 
 ('Viscara', 2, 1, 5, 0), 
 ('Viscara', 3, 20, 15, 51);
INSERT INTO dbo.SpaceEncounter (Planet, Type, Chance, Difficulty, LootTable) VALUES 
 ('Tattooine', 4, 20, 10, 51), 
 ('Tattooine', 4, 5, 20, 51),  
 ('Tattooine', 4, 1, 30, 51), 
 ('Tattooine', 2, 1, 15, 0), 
 ('Tattooine', 3, 15, 15, 51), 
 ('Tattooine', 3, 5, 25, 51), 
 ('Tattooine', 3, 3, 35, 51);
 
-- Create the table for public starports. 
CREATE TABLE SpaceStarport (
  ID UniqueIdentifier,
  Planet nVarChar(32),
  Name nVarChar(32),
  CustomsDC int,
  Cost int,
  Waypoint nVarChar(32)
  );

-- Commit the table creation, so that we can load data into it below.  
GO

-- Insert the starport data.
INSERT INTO dbo.SpaceStarport (ID, Planet, Name, CustomsDC, Cost, Waypoint) VALUES 
 ('6D9E3CFE-70B1-4B77-8166-10C4F5B0DA9D', 'Viscara', 'Veles Starport', 20, 400, 'VISCARA_LANDING'), 
 ('E38D63C5-D595-4DC8-873A-35151229CD6F', 'Tattooine', 'Mos Eisley Starport', 5, 400, 'TATTOOINE_LANDING');

-- New Piloting skill.  Copy the 100-rank skill progresion from OneHanded. 
INSERT INTO dbo.Skill (ID, SkillCategoryID, Name, MaxRank, IsActive, Description, [Primary], Secondary, Tertiary, ContributesToSkillCap) VALUES
  (36, 5, 'Piloting', 100, 1, 'Ability to pilot speeders and starships, follow navigation charts and control starship systems.', 2, 6, 0, 1);

INSERT INTO dbo.SkillXPRequirement (SkillID, Rank, XP)
  SELECT 36, Rank, XP FROM dbo.SkillXPRequirement WHERE SkillID = 1;

-- TBD Add perks for the piloting skill here. 

-- New base permissions - Dock Starship and Fly Starship.
ALTER TABLE dbo.PCBasePermission ADD CanDockStarship bit NOT NULL DEFAULT(0), CanFlyStarship bit NOT NULL DEFAULT(0);

-- Allow players to log off in instances.
ALTER TABLE dbo.Player ADD LocationInstanceID uniqueidentifier;
