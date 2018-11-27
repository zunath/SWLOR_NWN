DECLARE @PerkLevelID INT 

-- Add Engineering mod perks - Combat
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
VALUES ( 156 ,    -- ID - int
         'Combat Mod Installation - Electronics' ,   -- Name - varchar(64)
         NULL ,    -- FeatID - int
         1 , -- IsActive - bit
         'Engineering.CombatModInstallationEngineering' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Enables the installation of red combat mods into electronics.' ,  -- Description - nvarchar(256)
         33 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         NULL ,  -- ItemResref - nvarchar(16)
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL      -- CastAnimationID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         1 , -- Level - int
         2 , -- Price - int
         N'Install red mods up to level 5 on items up to level 10.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         5   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         2 , -- Level - int
         2 , -- Price - int
         N'Install red mods up to level 10 on items up to level 20.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         10   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         3 , -- Level - int
         2 , -- Price - int
         N'Install red mods up to level 15 on items up to level 30.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         15   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         4 , -- Level - int
         3 , -- Price - int
         N'Install red mods up to level 20 on items up to level 40.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         20   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         5 , -- Level - int
         3 , -- Price - int
         N'Install red mods up to level 25 on items up to level 50.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         25   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         6 , -- Level - int
         4 , -- Price - int
         N'Install red mods up to level 30 on items up to level 60.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         30   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         7 , -- Level - int
         4 , -- Price - int
         N'Install red mods up to level 35 on items up to level 70.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         35   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         8 , -- Level - int
         5 , -- Price - int
         N'Install red mods up to level 40 on items up to level 80.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         40   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         9 , -- Level - int
         5 , -- Price - int
         N'Install red mods up to level 45 on items up to level 90.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         45   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 156 , -- PerkID - int
         10 , -- Level - int
         5 , -- Price - int
         N'Install red mods up to level 50 on items up to level 100.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         50   -- RequiredRank - int
    )


-- Add Engineering mod perks - Force
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
VALUES ( 157 ,    -- ID - int
         'Force Mod Installation - Electronics' ,   -- Name - varchar(64)
         NULL ,    -- FeatID - int
         1 , -- IsActive - bit
         'Engineering.ForceModInstallationEngineering' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Enables the installation of blue force mods into electronics.' ,  -- Description - nvarchar(256)
         33 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         NULL ,  -- ItemResref - nvarchar(16)
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL      -- CastAnimationID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         1 , -- Level - int
         2 , -- Price - int
         N'Install blue mods up to level 5 on items up to level 10.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         5   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         2 , -- Level - int
         2 , -- Price - int
         N'Install blue mods up to level 10 on items up to level 20.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         10   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         3 , -- Level - int
         2 , -- Price - int
         N'Install blue mods up to level 15 on items up to level 30.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         15   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         4 , -- Level - int
         3 , -- Price - int
         N'Install blue mods up to level 20 on items up to level 40.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         20   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         5 , -- Level - int
         3 , -- Price - int
         N'Install blue mods up to level 25 on items up to level 50.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         25   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         6 , -- Level - int
         4 , -- Price - int
         N'Install blue mods up to level 30 on items up to level 60.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         30   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         7 , -- Level - int
         4 , -- Price - int
         N'Install blue mods up to level 35 on items up to level 70.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         35   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         8 , -- Level - int
         5 , -- Price - int
         N'Install blue mods up to level 40 on items up to level 80.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         40   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         9 , -- Level - int
         5 , -- Price - int
         N'Install blue mods up to level 45 on items up to level 90.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         45   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 157 , -- PerkID - int
         10 , -- Level - int
         5 , -- Price - int
         N'Install blue mods up to level 50 on items up to level 100.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         50   -- RequiredRank - int
    )



-- Add Engineering mod perks - Crafting

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
VALUES ( 158 ,    -- ID - int
         'Crafting Mod Installation - Electronics' ,   -- Name - varchar(64)
         NULL ,    -- FeatID - int
         1 , -- IsActive - bit
         'Engineering.CraftingModInstallationEngineering' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Enables the installation of green force mods into electronics.' ,  -- Description - nvarchar(256)
         33 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         NULL ,  -- ItemResref - nvarchar(16)
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL      -- CastAnimationID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         1 , -- Level - int
         2 , -- Price - int
         N'Install green mods up to level 5 on items up to level 10.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         5   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         2 , -- Level - int
         2 , -- Price - int
         N'Install green mods up to level 10 on items up to level 20.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         10   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         3 , -- Level - int
         2 , -- Price - int
         N'Install green mods up to level 15 on items up to level 30.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         15   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         4 , -- Level - int
         3 , -- Price - int
         N'Install green mods up to level 20 on items up to level 40.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         20   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         5 , -- Level - int
         3 , -- Price - int
         N'Install green mods up to level 25 on items up to level 50.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         25   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         6 , -- Level - int
         4 , -- Price - int
         N'Install green mods up to level 30 on items up to level 60.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         30   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         7 , -- Level - int
         4 , -- Price - int
         N'Install green mods up to level 35 on items up to level 70.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         35   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         8 , -- Level - int
         5 , -- Price - int
         N'Install green mods up to level 40 on items up to level 80.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         40   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         9 , -- Level - int
         5 , -- Price - int
         N'Install green mods up to level 45 on items up to level 90.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         45   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 158 , -- PerkID - int
         10 , -- Level - int
         5 , -- Price - int
         N'Install green mods up to level 50 on items up to level 100.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         50   -- RequiredRank - int
    )


-- Add Engineering mod perks - Special

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
VALUES ( 159 ,    -- ID - int
         'Special Mod Installation - Electronics' ,   -- Name - varchar(64)
         NULL ,    -- FeatID - int
         1 , -- IsActive - bit
         'Engineering.SpecialModInstallationEngineering' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Enables the installation of yellow force mods into electronics.' ,  -- Description - nvarchar(256)
         33 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         NULL ,  -- ItemResref - nvarchar(16)
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL      -- CastAnimationID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         1 , -- Level - int
         2 , -- Price - int
         N'Install yellow mods up to level 5 on items up to level 10.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         5   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         2 , -- Level - int
         2 , -- Price - int
         N'Install yellow mods up to level 10 on items up to level 20.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         10   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         3 , -- Level - int
         2 , -- Price - int
         N'Install yellow mods up to level 15 on items up to level 30.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         15   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         4 , -- Level - int
         3 , -- Price - int
         N'Install yellow mods up to level 20 on items up to level 40.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         20   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         5 , -- Level - int
         3 , -- Price - int
         N'Install yellow mods up to level 25 on items up to level 50.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         25   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         6 , -- Level - int
         4 , -- Price - int
         N'Install yellow mods up to level 30 on items up to level 60.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         30   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         7 , -- Level - int
         4 , -- Price - int
         N'Install yellow mods up to level 35 on items up to level 70.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         35   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         8 , -- Level - int
         5 , -- Price - int
         N'Install yellow mods up to level 40 on items up to level 80.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         40   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         9 , -- Level - int
         5 , -- Price - int
         N'Install yellow mods up to level 45 on items up to level 90.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         45   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 159 , -- PerkID - int
         10 , -- Level - int
         5 , -- Price - int
         N'Install yellow mods up to level 50 on items up to level 100.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         50   -- RequiredRank - int
    )
