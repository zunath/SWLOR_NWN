
BEGIN TRAN 

DECLARE @BlueprintID INT = (SELECT MAX(CraftBlueprintID) FROM dbo.CraftBlueprints) + 1
DECLARE @Name NVARCHAR(64) = 'Polearm S'
DECLARE @CraftCategoryID INT = 6
DECLARE @ResrefPrefix NVARCHAR(16) = 'spear_'
DECLARE @Quantity INT = 1
DECLARE @SkillID INT = 12
DECLARE @CraftDeviceID INT = 2
DECLARE @PerkID INT = 84
DECLARE @MainComponentID INT = 6
DECLARE @MainMinimum INT = 1
DECLARE @SecondaryComponentID INT = 10
DECLARE @SecondaryMinimum INT = 1
DECLARE @TertiaryComponentID INT = 0
DECLARE @TertiaryMinimum INT = 0
DECLARE @BaseEnhancementSlots INT = 2
-- end config

DECLARE @NumberAdded INT = 0
-- insert base
INSERT INTO dbo.CraftBlueprints ( CraftBlueprintID ,
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
                                  EnhancementSlots )
VALUES ( @BlueprintID + @NumberAdded ,    -- CraftBlueprintID - bigint
         @CraftCategoryID ,    -- CraftCategoryID - bigint
         @NumberAdded * 10 ,    -- BaseLevel - int
         N'Basic ' + @Name ,  -- ItemName - nvarchar(64)
         @ResrefPrefix + 'b' ,  -- ItemResref - nvarchar(16)
         @Quantity ,    -- Quantity - int
         @SkillID ,    -- SkillID - int
         @CraftDeviceID ,    -- CraftDeviceID - int
         @PerkID ,    -- PerkID - int
         1 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         @MainComponentID ,    -- MainComponentTypeID - int
         @MainMinimum ,    -- MainMinimum - int
         @SecondaryComponentID ,    -- SecondaryComponentTypeID - int
         @SecondaryMinimum ,    -- SecondaryMinimum - int
         @TertiaryComponentID ,    -- TertiaryComponentTypeID - int
         @TertiaryMinimum ,    -- TertiaryMinimum - int
         @BaseEnhancementSlots      -- EnhancementSlots - int
    )
SET @NumberAdded = @NumberAdded + 1

-- Insert tier 1
INSERT INTO dbo.CraftBlueprints ( CraftBlueprintID ,
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
                                  EnhancementSlots )
VALUES ( @BlueprintID + @NumberAdded ,    -- CraftBlueprintID - bigint
         @CraftCategoryID ,    -- CraftCategoryID - bigint
         @NumberAdded * 10 ,    -- BaseLevel - int
         @Name + '1' ,  -- ItemName - nvarchar(64)
         @ResrefPrefix + '1' ,  -- ItemResref - nvarchar(16)
         @Quantity ,    -- Quantity - int
         @SkillID ,    -- SkillID - int
         @CraftDeviceID ,    -- CraftDeviceID - int
         @PerkID ,    -- PerkID - int
         3 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         @MainComponentID ,    -- MainComponentTypeID - int
         @MainMinimum ,    -- MainMinimum - int
         @SecondaryComponentID ,    -- SecondaryComponentTypeID - int
         @SecondaryMinimum ,    -- SecondaryMinimum - int
         @TertiaryComponentID ,    -- TertiaryComponentTypeID - int
         @TertiaryMinimum ,    -- TertiaryMinimum - int
         @BaseEnhancementSlots + 1      -- EnhancementSlots - int
    )
SET @NumberAdded = @NumberAdded + 1

-- Insert tier 2
INSERT INTO dbo.CraftBlueprints ( CraftBlueprintID ,
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
                                  EnhancementSlots )
VALUES ( @BlueprintID + @NumberAdded ,    -- CraftBlueprintID - bigint
         @CraftCategoryID ,    -- CraftCategoryID - bigint
         @NumberAdded * 10 ,    -- BaseLevel - int
         @Name + '2' ,  -- ItemName - nvarchar(64)
         @ResrefPrefix + '2' ,  -- ItemResref - nvarchar(16)
         @Quantity ,    -- Quantity - int
         @SkillID ,    -- SkillID - int
         @CraftDeviceID ,    -- CraftDeviceID - int
         @PerkID ,    -- PerkID - int
         5 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         @MainComponentID ,    -- MainComponentTypeID - int
         @MainMinimum ,    -- MainMinimum - int
         @SecondaryComponentID ,    -- SecondaryComponentTypeID - int
         @SecondaryMinimum ,    -- SecondaryMinimum - int
         @TertiaryComponentID ,    -- TertiaryComponentTypeID - int
         @TertiaryMinimum ,    -- TertiaryMinimum - int
         @BaseEnhancementSlots + 2      -- EnhancementSlots - int
    )
SET @NumberAdded = @NumberAdded + 1

-- Insert tier 3
INSERT INTO dbo.CraftBlueprints ( CraftBlueprintID ,
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
                                  EnhancementSlots )
VALUES ( @BlueprintID + @NumberAdded ,    -- CraftBlueprintID - bigint
         @CraftCategoryID ,    -- CraftCategoryID - bigint
         @NumberAdded * 10 ,    -- BaseLevel - int
         @Name + '3' ,  -- ItemName - nvarchar(64)
         @ResrefPrefix + '3' ,  -- ItemResref - nvarchar(16)
         @Quantity ,    -- Quantity - int
         @SkillID ,    -- SkillID - int
         @CraftDeviceID ,    -- CraftDeviceID - int
         @PerkID ,    -- PerkID - int
         7 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         @MainComponentID ,    -- MainComponentTypeID - int
         @MainMinimum ,    -- MainMinimum - int
         @SecondaryComponentID ,    -- SecondaryComponentTypeID - int
         @SecondaryMinimum ,    -- SecondaryMinimum - int
         @TertiaryComponentID ,    -- TertiaryComponentTypeID - int
         @TertiaryMinimum ,    -- TertiaryMinimum - int
         @BaseEnhancementSlots + 3      -- EnhancementSlots - int
    )
SET @NumberAdded = @NumberAdded + 1

-- Insert tier 4
INSERT INTO dbo.CraftBlueprints ( CraftBlueprintID ,
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
                                  EnhancementSlots )
VALUES ( @BlueprintID + @NumberAdded ,    -- CraftBlueprintID - bigint
         @CraftCategoryID ,    -- CraftCategoryID - bigint
         @NumberAdded * 10 ,    -- BaseLevel - int
         @Name + '4' ,  -- ItemName - nvarchar(64)
         @ResrefPrefix + '4' ,  -- ItemResref - nvarchar(16)
         @Quantity ,    -- Quantity - int
         @SkillID ,    -- SkillID - int
         @CraftDeviceID ,    -- CraftDeviceID - int
         @PerkID ,    -- PerkID - int
         9 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         @MainComponentID ,    -- MainComponentTypeID - int
         @MainMinimum ,    -- MainMinimum - int
         @SecondaryComponentID ,    -- SecondaryComponentTypeID - int
         @SecondaryMinimum ,    -- SecondaryMinimum - int
         @TertiaryComponentID ,    -- TertiaryComponentTypeID - int
         @TertiaryMinimum ,    -- TertiaryMinimum - int
         @BaseEnhancementSlots + 4      -- EnhancementSlots - int
    )
SET @NumberAdded = @NumberAdded + 1


SELECT * 
FROM dbo.CraftBlueprints
WHERE CraftBlueprintID >= @BlueprintID

-- rollback
-- commit