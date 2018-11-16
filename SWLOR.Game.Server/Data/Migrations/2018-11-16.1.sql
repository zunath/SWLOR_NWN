
-- Emitter: Drop to level 1
UPDATE dbo.CraftBlueprint
SET BaseLevel = 1
WHERE ID = 611

-- Power clusters: Drop to level 1
UPDATE dbo.CraftBlueprint
SET BaseLevel = 1
WHERE ID IN (
113,114,115,116,222)

-- Saber hilt: Drop to level 1
UPDATE dbo.CraftBlueprint
SET BaseLevel = 1
WHERE ID = 221

-- Lightsabers: Drop to level 2
UPDATE dbo.CraftBlueprint
SET BaseLevel = 2
WHERE ID IN( 211, 613, 622, 632)


-- Move ranged weapon core, pistol barrel, and rifle barrel to engineering device menu
UPDATE dbo.CraftBlueprint
SET CraftDeviceID = 4
WHERE ID IN (
100, 101, 102

)

-- Move the lightsaber blueprint perk to the engineering perk category.
UPDATE dbo.Perk
SET PerkCategoryID = 33
WHERE ID = 151



-- Add the electronics repair perk
INSERT INTO dbo.Perk ( ID ,
                       Name ,
                       FeatID ,
                       IsActive ,
                       ScriptName ,
                       BaseFPCost ,
                       BaseCastingTime ,
                       Description ,
                       PerkCategoryID ,
                       CooldownCategoryID ,
                       ExecutionTypeID ,
                       ItemResref ,
                       IsTargetSelfOnly ,
                       Enmity ,
                       EnmityAdjustmentRuleID ,
                       CastAnimationID )
VALUES ( 152 ,    -- ID - int
         'Electronic Repair' ,   -- Name - varchar(64)
         NULL ,    -- FeatID - int
         1 , -- IsActive - bit
         'Engineering.ElectronicRepair' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Enables the use of electronic repair kits used for firarms, lightsabers, and other mechanically-based items.' ,  -- Description - nvarchar(256)
         33 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         NULL ,  -- ItemResref - nvarchar(16)
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL      -- CastAnimationID - int
    )


DECLARE @ID INT 

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 152 , -- PerkID - int
         1 , -- Level - int
         3 , -- Price - int
         N'Can use tech 1 electronics repair kits.' -- Description - nvarchar(512)
    )

SET @ID = SCOPE_IDENTITY()


INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @ID , -- PerkLevelID - int
         22 , -- SkillID - int
         10   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 152 , -- PerkID - int
         2 , -- Level - int
         3 , -- Price - int
         N'Can use tech 2 electronics repair kits.' -- Description - nvarchar(512)
    )

SET @ID = SCOPE_IDENTITY()


INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @ID , -- PerkLevelID - int
         22 , -- SkillID - int
         20   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 152 , -- PerkID - int
         3 , -- Level - int
         3 , -- Price - int
         N'Can use tech 3 electronics repair kits.' -- Description - nvarchar(512)
    )

SET @ID = SCOPE_IDENTITY()


INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @ID , -- PerkLevelID - int
         22 , -- SkillID - int
         30   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 152 , -- PerkID - int
         4 , -- Level - int
         3 , -- Price - int
         N'Can use tech 4 electronics repair kits.' -- Description - nvarchar(512)
    )

SET @ID = SCOPE_IDENTITY()


INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @ID , -- PerkLevelID - int
         22 , -- SkillID - int
         40   -- RequiredRank - int
    )