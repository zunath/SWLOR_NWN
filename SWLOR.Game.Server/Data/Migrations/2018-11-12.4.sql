
INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 54 , -- ID - int
         N'Water' -- Name - nvarchar(32)
    )
	
INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 55 , -- ID - int
         N'Curry Paste' -- Name - nvarchar(32)
    )
INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 56 , -- ID - int
         N'Soup' -- Name - nvarchar(32)
    )
INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 57 , -- ID - int
         N'Spiced Milk' -- Name - nvarchar(32)
    )
INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 58 , -- ID - int
         N'Dough' -- Name - nvarchar(32)
    )
INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 59 , -- ID - int
         N'Butter' -- Name - nvarchar(32)
    )
INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 60 , -- ID - int
         N'Noodles' -- Name - nvarchar(32)
    )
INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 61 , -- ID - int
         N'Eggs' -- Name - nvarchar(32)
    )

INSERT INTO dbo.ComponentType ( ID ,
                                Name )
VALUES ( 62 , -- ID - int
         N'Emitter' -- Name - nvarchar(32)
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
VALUES ( 611 ,    -- ID - int
         13 ,    -- CraftCategoryID - int
         5 ,    -- BaseLevel - int
         N'Emitter' ,  -- ItemName - nvarchar(64)
         N'emitter' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         22 ,    -- SkillID - int
         4 ,    -- CraftDeviceID - int
         151 ,    -- PerkID - int
         1 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         15 ,    -- MainComponentTypeID - int
         2 ,    -- MainMinimum - int
         0 ,    -- SecondaryComponentTypeID - int
         0 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         3 ,    -- EnhancementSlots - int
         4 ,    -- MainMaximum - int
         0 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         NULL      -- BaseStructureID - int
    )

-- Update existing lightsabers to have the color blue in the name,
-- change the components needed to an emitter, crystal cluster, and hilt (hilt is already set up)
UPDATE dbo.CraftBlueprint
SET ItemName = ItemName + ' (Blue)',
	MainComponentTypeID = 62,
	TertiaryComponentTypeID = 28,
	TertiaryMinimum = 1,
	TertiaryMaximum = 2
WHERE ID IN (211, 212,213,214,215,216,217,218,219,220)


-- Add the red, green, and yellow sabers
INSERT INTO dbo.CraftBlueprint ( ID ,CraftCategoryID ,BaseLevel ,ItemName ,ItemResref ,Quantity ,SkillID ,CraftDeviceID ,PerkID ,RequiredPerkLevel ,IsActive ,MainComponentTypeID ,MainMinimum ,SecondaryComponentTypeID ,SecondaryMinimum ,TertiaryComponentTypeID ,TertiaryMinimum ,EnhancementSlots ,MainMaximum ,SecondaryMaximum ,TertiaryMaximum ,BaseStructureID )
SELECT 611 + ROW_NUMBER() OVER (ORDER BY ID) ,
       CraftCategoryID ,
       BaseLevel ,
       REPLACE(ItemName, '(Blue)', '(Red)') ,
       CASE 
			WHEN ItemResref LIKE 'lightsaber_%'
				THEN REPLACE(ItemResref, 'lightsaber_', 'lightsaber_r_')
			WHEN ItemResref LIKE 'saberstaff_%'
				THEN REPLACE(ItemResref, 'saberstaff_', 'saberstaff_r_')
			ELSE 'ERROR'
	   END,
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
       BaseStructureID 
FROM dbo.CraftBlueprint 
WHERE ID IN (211,212,213,214,215,216,217,218,219,220)


INSERT INTO dbo.CraftBlueprint ( ID ,CraftCategoryID ,BaseLevel ,ItemName ,ItemResref ,Quantity ,SkillID ,CraftDeviceID ,PerkID ,RequiredPerkLevel ,IsActive ,MainComponentTypeID ,MainMinimum ,SecondaryComponentTypeID ,SecondaryMinimum ,TertiaryComponentTypeID ,TertiaryMinimum ,EnhancementSlots ,MainMaximum ,SecondaryMaximum ,TertiaryMaximum ,BaseStructureID )
SELECT 621 + ROW_NUMBER() OVER (ORDER BY ID) ,
       CraftCategoryID ,
       BaseLevel ,
       REPLACE(ItemName, '(Blue)', '(Green)') ,
       CASE 
			WHEN ItemResref LIKE 'lightsaber_%'
				THEN REPLACE(ItemResref, 'lightsaber_', 'lightsaber_g_')
			WHEN ItemResref LIKE 'saberstaff_%'
				THEN REPLACE(ItemResref, 'saberstaff_', 'saberstaff_g_')
			ELSE 'ERROR'
	   END,
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
       BaseStructureID 
FROM dbo.CraftBlueprint 
WHERE ID IN (211,212,213,214,215,216,217,218,219,220)

INSERT INTO dbo.CraftBlueprint ( ID ,CraftCategoryID ,BaseLevel ,ItemName ,ItemResref ,Quantity ,SkillID ,CraftDeviceID ,PerkID ,RequiredPerkLevel ,IsActive ,MainComponentTypeID ,MainMinimum ,SecondaryComponentTypeID ,SecondaryMinimum ,TertiaryComponentTypeID ,TertiaryMinimum ,EnhancementSlots ,MainMaximum ,SecondaryMaximum ,TertiaryMaximum ,BaseStructureID )
SELECT 631 + ROW_NUMBER() OVER (ORDER BY ID) ,
       CraftCategoryID ,
       BaseLevel ,
       REPLACE(ItemName, '(Blue)', '(Yellow)') ,
       CASE 
			WHEN ItemResref LIKE 'lightsaber_%'
				THEN REPLACE(ItemResref, 'lightsaber_', 'lightsaber_y_')
			WHEN ItemResref LIKE 'saberstaff_%'
				THEN REPLACE(ItemResref, 'saberstaff_', 'saberstaff_y_')
			ELSE 'ERROR'
	   END,
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
       BaseStructureID 
FROM dbo.CraftBlueprint 
WHERE ID IN (211,212,213,214,215,216,217,218,219,220)


-- Move the quest requirement for lightsaber crafting to ID 30 (the new force quest).
UPDATE dbo.PerkLevelQuestRequirement
SET RequiredQuestID = 30
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID = 151 
)

-- Remove the old, temporary quest which was locking lightsaber crafting
DELETE FROM dbo.QuestState
WHERE QuestID = 24

DELETE FROM dbo.Quest
WHERE ID = 24


-- Move the lightsaber blueprint perk to the right category (Engineering)
UPDATE dbo.Perk
SET PerkCategoryID = 33
WHERE ID = 151