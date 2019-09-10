
EXEC sys.sp_rename @objname = N'ClientLogEvent' ,  -- nvarchar(1035)
                   @newname = 'ModuleEvent' 
GO

EXEC sys.sp_rename @objname = N'ClientLogEventType' ,  -- nvarchar(1035)
                   @newname = 'ModuleEventType'
GO

EXEC sys.sp_rename @objname = N'ModuleEvent.ClientLogEventTypeID' ,  -- nvarchar(1035)
                   @newname = 'ModuleEventTypeID' , -- sysname
                   @objtype = 'COLUMN'     -- varchar(13)
GO  


ALTER TABLE dbo.ModuleEvent
ADD BankID INT NULL 

ALTER TABLE dbo.ModuleEvent
ADD ItemID UNIQUEIDENTIFIER NULL

ALTER TABLE dbo.ModuleEvent
ADD ItemName NVARCHAR(MAX) NULL

ALTER TABLE dbo.ModuleEvent
ADD ItemTag NVARCHAR(64) NULL

ALTER TABLE dbo.ModuleEvent
ADD ItemResref NVARCHAR(16) NULL

ALTER TABLE dbo.ModuleEvent
ADD PCBaseID UNIQUEIDENTIFIER NULL

ALTER TABLE dbo.ModuleEvent
ADD PCBaseStructureID UNIQUEIDENTIFIER NULL

ALTER TABLE dbo.ModuleEvent
ADD BaseStructureID INT NULL

ALTER TABLE dbo.ModuleEvent
ADD CustomName NVARCHAR(64) NULL

ALTER TABLE dbo.ModuleEvent
ADD AreaSector CHAR(2) NULL

ALTER TABLE dbo.ModuleEvent
ADD AreaName NVARCHAR(128) NULL

ALTER TABLE dbo.ModuleEvent
ADD AreaTag NVARCHAR(32) NULL 

ALTER TABLE dbo.ModuleEvent
ADD AreaResref NVARCHAR(32) NULL

ALTER TABLE dbo.ModuleEvent
ADD PCBaseTypeID INT NULL

ALTER TABLE dbo.ModuleEvent
ADD DateRentDue DATETIME2 NULL

ALTER TABLE dbo.ModuleEvent
ADD AttackerPlayerID UNIQUEIDENTIFIER NULL

UPDATE dbo.ModuleEventType
SET Name = 'Player Log In'
WHERE ID = 1

UPDATE dbo.ModuleEventType
SET Name = 'Player Log Out'
WHERE ID = 2

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 3 , -- ID - int
         N'Player Death' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 4 , -- ID - int
         N'Player Respawn' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 5 , -- ID - int
         N'Bank Item Stored' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 6 , -- ID - int
         N'Bank Item Retrieved' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 7 , -- ID - int
         N'Structure Item Stored' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 8 , -- ID - int
         N'Structure Item Retrieved' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 9 , -- ID - int
         N'Land Purchase' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 10 , -- ID - int
         N'Base Lease Expired' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 11 , -- ID - int
         N'Base Destroyed' -- Name - nvarchar(30)
    )

INSERT INTO dbo.ModuleEventType ( ID ,
                                  Name )
VALUES ( 12 , -- ID - int
         N'Base Lease Cancelled' -- Name - nvarchar(30)
    )
