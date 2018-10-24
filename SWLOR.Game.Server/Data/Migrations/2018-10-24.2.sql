
CREATE TABLE CustomEffectCategory(
	CustomEffectCategoryID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL DEFAULT ''
)

GO 

ALTER TABLE dbo.CustomEffects
ADD CustomEffectCategoryID INT NOT NULL DEFAULT 1
GO

INSERT INTO dbo.CustomEffectCategory ( CustomEffectCategoryID ,
                                   Name )
VALUES ( 1 , -- CustomEffectCategoryID - int
         N'Normal Effect' -- Name - nvarchar(32)
    )

INSERT INTO dbo.CustomEffectCategory ( CustomEffectCategoryID ,
                                   Name )
VALUES ( 2 , -- CustomEffectCategoryID - int
         N'Stance' -- Name - nvarchar(32)
    )

INSERT INTO dbo.CustomEffectCategory ( CustomEffectCategoryID ,
                                   Name )
VALUES ( 3 , -- CustomEffectCategoryID - int
         N'Food Effect' -- Name - nvarchar(32)
    )

GO

ALTER TABLE dbo.CustomEffects
ADD CONSTRAINT FK_CustomEffects_CustomEffectCategoryID FOREIGN KEY(CustomEffectCategoryID)
	REFERENCES dbo.CustomEffectCategory(CustomEffectCategoryID)

GO

EXEC dbo.ADM_Drop_Column @TableName = N'CustomEffects' , -- nvarchar(200)
                         @ColumnName = N'IsStance'  -- nvarchar(200)

--GO 

UPDATE dbo.CustomEffects
SET CustomEffectCategoryID = 2
WHERE CustomEffectID IN (
15,
22,
23
)
