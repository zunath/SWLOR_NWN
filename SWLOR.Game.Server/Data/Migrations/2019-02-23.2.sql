
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 124 ,   -- ID - int
         N'Starship Equipment' , -- Name - nvarchar(64)
         1  -- IsActive - bit
    )


	
INSERT INTO dbo.CraftBlueprintCategory ( ID ,
                                         Name ,
                                         IsActive )
VALUES ( 47 ,   -- ID - int
         N'Special Furniture' , -- Name - nvarchar(32)
         1  -- IsActive - bit
    )

UPDATE dbo.CraftBlueprint
SET CraftCategoryID = 47,
	MainMaximum = 2
WHERE ID = 682