
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

-- Add default structure mode ID to existing building structures
ALTER TABLE dbo.BaseStructure
ADD DefaultStructureModeID INT NOT NULL DEFAULT 0
CONSTRAINT FK_BaseStructure_DefaultStructureModeID FOREIGN KEY(DefaultStructureModeID)
	REFERENCES dbo.StructureMode(ID)

GO

UPDATE dbo.BaseStructure
SET DefaultStructureModeID = 1
WHERE ID IN (153, 154, 155)



INSERT INTO dbo.BaseStructureType ( ID ,
                                    Name ,
                                    IsActive ,
                                    CanPlaceInside ,
                                    CanPlaceOutside )
VALUES ( 11 ,    -- ID - int
         N'Crafting Device' ,  -- Name - nvarchar(64)
         1 , -- IsActive - bit
         1 , -- CanPlaceInside - bit
         0   -- CanPlaceOutside - bit
    )

INSERT INTO dbo.BaseStructureType ( ID ,
                                    Name ,
                                    IsActive ,
                                    CanPlaceInside ,
                                    CanPlaceOutside )
VALUES ( 12 ,    -- ID - int
         N'Persistent Storage' ,  -- Name - nvarchar(64)
         1 , -- IsActive - bit
         1 , -- CanPlaceInside - bit
         0   -- CanPlaceOutside - bit
    )

-- Move crafting devices to Crafting Device type
UPDATE dbo.BaseStructure
SET BaseStructureTypeID = 11
WHERE ID IN (
	147, 149, 150, 176, 177, 148, 146
)

-- Move containers to Persistent Storage type
UPDATE dbo.BaseStructure
SET BaseStructureTypeID = 12
WHERE BaseStructureTypeID = 8
	AND Storage > 0
