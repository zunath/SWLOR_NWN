
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
