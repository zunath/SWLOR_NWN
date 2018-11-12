
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
VALUES ( 177 ,    -- ID - int
         8 ,    -- BaseStructureTypeID - int
         N'Medical Terminal' ,  -- Name - nvarchar(64)
         N'medical_term' ,  -- PlaceableResref - nvarchar(16)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 , -- IsActive - bit
         0.0 ,  -- Power - float
         0.0 ,  -- CPU - float
         0.0 ,  -- Durability - float
         0 ,    -- Storage - int
         0 , -- HasAtmosphere - bit
         0 ,    -- ReinforcedStorage - int
         1 , -- RequiresBasePower - bit
         0 ,    -- ResourceStorage - int
         0      -- RetrievalRating - int
    )

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
VALUES ( 610 ,    -- ID - int
         39 ,    -- CraftCategoryID - int
         0 ,    -- BaseLevel - int
         N'Medical Terminal' ,  -- ItemName - nvarchar(64)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         15 ,    -- SkillID - int
         5 ,    -- CraftDeviceID - int
         2 ,    -- PerkID - int
         1 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         2 ,    -- MainComponentTypeID - int
         2 ,    -- MainMinimum - int
         3 ,    -- SecondaryComponentTypeID - int
         2 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         0 ,    -- EnhancementSlots - int
         2 ,    -- MainMaximum - int
         2 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         177      -- BaseStructureID - int
    )
