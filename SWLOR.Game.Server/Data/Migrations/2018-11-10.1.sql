
INSERT INTO dbo.BaseStructure ( ID ,
                                BaseStructureTypeID ,
                                Name ,
                                PlaceableResref ,
                                ItemResref ,
                                IsActive ,
                                Power ,
                                CPU ,
                                Durability ,
                                Storage ,
                                HasAtmosphere ,
                                ReinforcedStorage ,
                                RequiresBasePower ,
                                ResourceStorage ,
                                RetrievalRating )
VALUES ( 13 ,    -- ID - int
         8 ,    -- BaseStructureTypeID - int
         N'Gong' ,  -- Name - nvarchar(64)
         N'plc_gong' ,  -- PlaceableResref - nvarchar(16)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 , -- IsActive - bit
         0.0 ,  -- Power - float
         0.0 ,  -- CPU - float
         0.0 ,  -- Durability - float
         0 ,    -- Storage - int
         1 , -- HasAtmosphere - bit
         0 ,    -- ReinforcedStorage - int
         0 , -- RequiresBasePower - bit
         0 ,    -- ResourceStorage - int
         0      -- RetrievalRating - int
    )

GO


INSERT INTO dbo.CraftBlueprint ( ID ,
                                 CraftCategoryID ,
                                 BaseLevel ,
                                 ItemName ,
                                 ItemResref ,
                                 Quantity ,
                                 SkillID ,
                                 CraftDeviceID ,
                                 PerkID ,
                                 RequiredPerkLevel ,
                                 IsActive ,
                                 MainComponentTypeID ,
                                 MainMinimum ,
                                 SecondaryComponentTypeID ,
                                 SecondaryMinimum ,
                                 TertiaryComponentTypeID ,
                                 TertiaryMinimum ,
                                 EnhancementSlots ,
                                 MainMaximum ,
                                 SecondaryMaximum ,
                                 TertiaryMaximum ,
                                 BaseStructureID )
VALUES ( 331 ,    -- ID - int
         32 ,    -- CraftCategoryID - int
         0 ,    -- BaseLevel - int
         N'Gong' ,  -- ItemName - nvarchar(64)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         15 ,    -- SkillID - int
         5 ,    -- CraftDeviceID - int
         2 ,    -- PerkID - int
         1 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         2 ,    -- MainComponentTypeID - int
         2 ,    -- MainMinimum - int
         0 ,    -- SecondaryComponentTypeID - int
         0 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         0 ,    -- EnhancementSlots - int
         4 ,    -- MainMaximum - int
         0 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         13      -- BaseStructureID - int
    )
