
CREATE TABLE StructureMode(
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL
)

GO

INSERT INTO dbo.StructureMode ( ID ,
                                Name )
VALUES ( 0 , -- ID - int
         N'None' -- Name - nvarchar(32)
    )

INSERT INTO dbo.StructureMode ( ID ,
                                Name )
VALUES ( 1 , -- ID - int
         N'Residence' -- Name - nvarchar(32)
    )

INSERT INTO dbo.StructureMode ( ID ,
                                Name )
VALUES ( 2 , -- ID - int
         N'Workshop' -- Name - nvarchar(32)
    )

INSERT INTO dbo.StructureMode ( ID ,
                                Name )
VALUES ( 3 , -- ID - int
         N'Storefront' -- Name - nvarchar(32)
    )

GO


ALTER TABLE dbo.PCBaseStructure
ADD StructureModeID INT NOT NULL DEFAULT 0
CONSTRAINT FK_PCBaseStructure_StructureModeID FOREIGN KEY(StructureModeID)
	REFERENCES dbo.StructureMode(ID)
GO


UPDATE dbo.PCBaseStructure
SET StructureModeID = 1
WHERE BaseStructureID IN (153, 154, 155)
GO



-- Add permissions for changing structure mode

ALTER TABLE dbo.PCBasePermission
ADD CanChangeStructureMode BIT NOT NULL DEFAULT 0

ALTER TABLE dbo.PCBaseStructurePermission
ADD CanChangeStructureMode BIT NOT NULL DEFAULT 0

GO

-- Only owners will get this new permission by default
UPDATE dbo.PCBasePermission
SET CanChangeStructureMode = 1
WHERE CanAdjustPermissions = 1

UPDATE dbo.PCBaseStructurePermission
SET CanChangeStructureMode = 1
WHERE CanAdjustPermissions = 1