

PRINT('Adding proc ADM_Drop_Constraint')
GO

CREATE PROCEDURE [dbo].[ADM_Drop_Constraint](@TableName nvarchar(200), @ColumnName nvarchar(200))
AS
BEGIN
	DECLARE @ConstraintName nvarchar(200)
	SELECT @ConstraintName = Name FROM SYS.DEFAULT_CONSTRAINTS
	WHERE PARENT_OBJECT_ID = OBJECT_ID(@TableName)
	AND PARENT_COLUMN_ID = (SELECT column_id FROM sys.columns
							WHERE NAME = @ColumnName
							AND object_id = OBJECT_ID(@TableName))
	
	IF @ConstraintName IS NOT NULL
	EXEC('ALTER TABLE '+ @TableName +' DROP CONSTRAINT ' + @ConstraintName)
END

GO
PRINT('Creating table CustomEffectCategory')
GO

CREATE TABLE CustomEffectCategory(
	CustomEffectCategoryID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL DEFAULT ''
)

GO 

PRINT('Adding CustomEffectCategoryID to CustomEffects table')
GO
ALTER TABLE dbo.CustomEffects
ADD CustomEffectCategoryID INT NOT NULL DEFAULT 1
GO

PRINT('Inserting normal effect record')
GO
INSERT INTO dbo.CustomEffectCategory ( CustomEffectCategoryID ,
                                   Name )
VALUES ( 1 , -- CustomEffectCategoryID - int
         N'Normal Effect' -- Name - nvarchar(32)
    )

GO

PRINT('Inserting stance effect record')
GO
INSERT INTO dbo.CustomEffectCategory ( CustomEffectCategoryID ,
                                   Name )
VALUES ( 2 , -- CustomEffectCategoryID - int
         N'Stance' -- Name - nvarchar(32)
    )

GO


PRINT('Inserting food effect record')
GO
INSERT INTO dbo.CustomEffectCategory ( CustomEffectCategoryID ,
                                   Name )
VALUES ( 3 , -- CustomEffectCategoryID - int
         N'Food Effect' -- Name - nvarchar(32)
    )

GO


PRINT('Adding FK to CustomEffects')
GO
ALTER TABLE dbo.CustomEffects
ADD CONSTRAINT FK_CustomEffects_CustomEffectCategoryID FOREIGN KEY(CustomEffectCategoryID)
	REFERENCES dbo.CustomEffectCategory(CustomEffectCategoryID)

GO


PRINT('Dropping column IsStance')
GO
EXEC dbo.ADM_Drop_Column @TableName = N'CustomEffects' , -- nvarchar(200)
                         @ColumnName = N'IsStance'  -- nvarchar(200)

GO 


PRINT('Updating custom effect ID')
GO
UPDATE dbo.CustomEffects
SET CustomEffectCategoryID = 2
WHERE CustomEffectID IN (
15,
22,
23
)

GO



PRINT('Adding Food effect')
GO
INSERT INTO dbo.CustomEffects ( CustomEffectID ,
                                Name ,
                                IconID ,
                                ScriptHandler ,
                                StartMessage ,
                                ContinueMessage ,
                                WornOffMessage ,
                                CustomEffectCategoryID )
VALUES ( 25 ,   -- CustomEffectID - bigint
         N'Food Effect' , -- Name - nvarchar(32)
         0 ,   -- IconID - int
         N'FoodEffect' , -- ScriptHandler - nvarchar(64)
         N'You eat a meal.' , -- StartMessage - nvarchar(64)
         N'' , -- ContinueMessage - nvarchar(64)
         N'You are no longer well-fed.' , -- WornOffMessage - nvarchar(64)
         3     -- CustomEffectCategoryID - int
    )
GO


PRINT('Dropping constraint on Data field of PCCustomEffects')
GO
EXEC dbo.ADM_Drop_Constraint @TableName = N'PCCustomEffects' , -- nvarchar(200)
                             @ColumnName = N'Data'  -- nvarchar(200)
GO


PRINT('Changing size of Data field in PCCustomEffects')
GO
ALTER TABLE dbo.PCCustomEffects
ALTER COLUMN Data NVARCHAR(MAX) NOT NULL
GO


PRINT('Adding default constraint to Data field in PCCustomEffects')
GO
ALTER TABLE dbo.PCCustomEffects
ADD CONSTRAINT DF_PCCustomEffects_Data DEFAULT '' FOR Data
