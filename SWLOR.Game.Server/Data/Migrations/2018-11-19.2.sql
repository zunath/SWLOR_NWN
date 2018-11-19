
CREATE TABLE DMActionType(
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL
)

INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 1 , -- ID - int
         N'Spawn Creature' -- Name - nvarchar(32)
    )
	
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 2 , -- ID - int
         N'Spawn Item' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 3 , -- ID - int
         N'Spawn Trigger' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 4 , -- ID - int
         N'Spawn Waypoint' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 5 , -- ID - int
         N'Spawn Encounter' -- Name - nvarchar(32)
    )
	
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 6 , -- ID - int
         N'Spawn Portal' -- Name - nvarchar(32)
    )
	
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 7 , -- ID - int
         N'Spawn Placeable' -- Name - nvarchar(32)
    )
	
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 8 , -- ID - int
         N'Change Difficulty' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 9 , -- ID - int
         N'Spawn Trap' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 10 , -- ID - int
         N'Heal' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 11 , -- ID - int
         N'Kill' -- Name - nvarchar(32)
    )
	
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 12 , -- ID - int
         N'Jump' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 13 , -- ID - int
         N'Possess' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 14 , -- ID - int
         N'Toggle Immortal' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 15 , -- ID - int
         N'Force Rest' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 16 , -- ID - int
         N'Limbo' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 17 , -- ID - int
         N'Toggle AI' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 18 , -- ID - int
         N'Toggle Lock' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 19 , -- ID - int
         N'Disable Trap' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 20 , -- ID - int
         N'Appear' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 21 , -- ID - int
         N'Disappear' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 22 , -- ID - int
         N'Give XP' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 23 , -- ID - int
         N'Give Level' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 24 , -- ID - int
         N'Give Gold' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 25 , -- ID - int
         N'Give Item' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 26 , -- ID - int
         N'Take Item' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 27 , -- ID - int
         N'Jump Target to Point' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 28 , -- ID - int
         N'Jump All Players' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 29 , -- ID - int
         N'Set Stat' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 30 , -- ID - int
         N'Get Variable' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 31 , -- ID - int
         N'Set Variable' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 32 , -- ID - int
         N'Set Time' -- Name - nvarchar(32)
    )
INSERT INTO dbo.DMActionType ( ID ,
                               Name )
VALUES ( 33 , -- ID - int
         N'Set Date' -- Name - nvarchar(32)
    )
GO

CREATE TABLE DMAction(
	ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	DMActionTypeID INT NOT NULL,
	Name NVARCHAR(128) NOT NULL,
	CDKey NVARCHAR(20) NOT NULL, 
	DateOfAction DATETIME2 NOT NULL,
	Details NVARCHAR(MAX) NOT NULL DEFAULT '',

	CONSTRAINT FK_DMAction_DMActionTypeID FOREIGN KEY(DMActionTypeID)
		REFERENCES dbo.DMActionType(ID)
)
