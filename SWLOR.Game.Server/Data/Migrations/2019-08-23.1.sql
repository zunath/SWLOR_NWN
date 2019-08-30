
-- Remove the AreaBakeStep column from server configuration. It's a constant in AreaService now.
EXEC dbo.ADM_Drop_Constraint @TableName = N'dbo.ServerConfiguration' , -- nvarchar(200)
                             @ColumnName = N'AreaBakeStep'  -- nvarchar(200)

ALTER TABLE dbo.ServerConfiguration
DROP COLUMN AreaBakeStep

-- Walkmeshes aren't stored in the DB anymore since they have to be calculated on boot anyway.
EXEC dbo.ADM_Drop_Constraint @TableName = N'dbo.Area' , -- nvarchar(200)
                             @ColumnName = N'Walkmesh'  -- nvarchar(200)

ALTER TABLE dbo.Area
DROP COLUMN Walkmesh

-- We don't store baking information in the DB anymore since it happens on boot.
EXEC dbo.ADM_Drop_Constraint @TableName = N'dbo.Area' , -- nvarchar(200)
                             @ColumnName = N'DateLastBaked'  -- nvarchar(200)

ALTER TABLE dbo.Area
DROP COLUMN DateLastBaked

-- The walkmesh table isn't useful anymore because all of that information is stored at boot time in memory.
DROP TABLE dbo.AreaWalkmesh
