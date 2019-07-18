
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
