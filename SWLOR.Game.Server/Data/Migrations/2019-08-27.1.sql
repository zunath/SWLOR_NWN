
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
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (5, 'Tattooine', 4, 20, 10, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (6, 'Tattooine', 4, 5, 20, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (7, 'Tattooine', 4, 1, 30, 53)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (8, 'Tattooine', 2, 1, 15, 0)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (9, 'Tattooine', 3, 15, 15, 51)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (10, 'Tattooine', 3, 5, 25, 51)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (11, 'Tattooine', 3, 3, 35, 51)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (12, 'Mon Cala', 1, 15, 40, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (13, 'Mon Cala', 2, 1, 40, 0)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (14, 'Mon Cala', 3, 15, 45, 0)

-- These are new for Hutlar.
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (15, 'Hutlar', 1, 15, 40, 52)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (16, 'Hutlar', 2, 1, 40, 0)
INSERT INTO dbo.SpaceEncounter(ID, Planet, TypeID, Chance, Difficulty, LootTableID) VALUES (17, 'Hutlar', 3, 15, 45, 0)