
CREATE TABLE Starport(
	ID INT NOT NULL PRIMARY KEY IDENTITY,
	StarportID UNIQUEIDENTIFIER NOT NULL UNIQUE,
	PlanetName NVARCHAR(64) NOT NULL,
	Name NVARCHAR(64) NOT NULL,
	CustomsDC INT NOT NULL,
	Cost INT NOT NULL,
	WaypointTag NVARCHAR(32) NOT NULL
)
GO

INSERT INTO dbo.Starport ( StarportID ,
                           PlanetName ,
                           Name ,
                           CustomsDC ,
                           Cost ,
                           WaypointTag )
SELECT ID ,
       Planet ,
       Name ,
       CustomsDC ,
       Cost ,
       Waypoint 
FROM dbo.SpaceStarport 

GO

-- Fixes spelling on Tatooine
UPDATE dbo.Starport
SET PlanetName = 'Tatooine', WaypointTag = 'TATOOINE_LANDING' WHERE ID = 2

DROP TABLE dbo.SpaceStarport
GO


INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 0 , -- ID - int
         N'Invalid' -- Name - nvarchar(64)
    )


DROP TABLE dbo.SpaceEncounter
GO

CREATE TABLE SpaceEncounter(
	ID INT PRIMARY KEY NOT NULL,
	Planet NVARCHAR(255) NOT NULL,
	TypeID INT NOT NULL,
	Chance INT NOT NULL,
	Difficulty INT NOT NULL,
	LootTableID INT NOT NULL,

	CONSTRAINT FK_SpaceEncounter_LootTableID FOREIGN KEY(LootTableID)
		REFERENCES dbo.LootTable(ID)
) 
GO

INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (1, 'Viscara', 1, 15, 10, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (2, 'Viscara', 1, 4, 20, 53)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (3, 'Viscara', 2, 1, 5, 0)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (4, 'Viscara', 3, 20, 15, 51)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (5, 'Tatooine', 4, 20, 10, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (6, 'Tatooine', 4, 5, 20, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (7, 'Tatooine', 4, 1, 30, 53)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (8, 'Tatooine', 2, 1, 15, 0)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (9, 'Tatooine', 3, 15, 15, 51)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (10, 'Tatooine', 3, 5, 25, 51)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (11, 'Tatooine', 3, 3, 35, 51)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (12, 'Mon Cala', 1, 15, 40, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (13, 'Mon Cala', 2, 1, 40, 0)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (14, 'Mon Cala', 3, 15, 45, 0)

-- These are new for Hutlar.
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (15, 'Hutlar', 1, 15, 40, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (16, 'Hutlar', 2, 1, 40, 0)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (17, 'Hutlar', 3, 15, 45, 0)


GO


-- Resource spawn tables for Hutlar Qion Tundra
INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 33 ,   -- ID - int
         N'Hutlar - Resources' , -- Name - nvarchar(64)
         64     -- SpawnObjectTypeID - int
    )

INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID ,
                              AIFlags )
VALUES ( 77 ,   -- ID - int
         33 ,   -- SpawnID - int
         N'crystal_clusterb' , -- Resref - nvarchar(16)
         10 ,   -- Weight - int
         N'ColoredCrystalSpawnRule' , -- SpawnRule - nvarchar(32)
         NULL ,   -- NPCGroupID - int
         N'' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         0     -- AIFlags - int
    )
INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID ,
                              AIFlags )
VALUES ( 82 ,   -- ID - int
         33 ,   -- SpawnID - int
         N'crystal_clusterr' , -- Resref - nvarchar(16)
         8 ,   -- Weight - int
         N'ColoredCrystalSpawnRule' , -- SpawnRule - nvarchar(32)
         NULL ,   -- NPCGroupID - int
         N'' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         0     -- AIFlags - int
    )
INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID ,
                              AIFlags )
VALUES ( 83 ,   -- ID - int
         33 ,   -- SpawnID - int
         N'crystal_clustery' , -- Resref - nvarchar(16)
         6 ,   -- Weight - int
         N'ColoredCrystalSpawnRule' , -- SpawnRule - nvarchar(32)
         NULL ,   -- NPCGroupID - int
         N'' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         0     -- AIFlags - int
    )
INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID ,
                              AIFlags )
VALUES ( 84 ,   -- ID - int
         33 ,   -- SpawnID - int
         N'crystal_clusterg' , -- Resref - nvarchar(16)
         12 ,   -- Weight - int
         N'ColoredCrystalSpawnRule' , -- SpawnRule - nvarchar(32)
         NULL ,   -- NPCGroupID - int
         N'' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         0     -- AIFlags - int
    )


	
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 116 ,    -- LootTableID - int
         'elec_slime' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 116 ,    -- LootTableID - int
         'slug_bile' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         6 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 116 ,    -- LootTableID - int
         'slug_tooth' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         8 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )