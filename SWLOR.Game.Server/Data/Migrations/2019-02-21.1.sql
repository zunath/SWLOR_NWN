
-- Increase reinforced storage to 7200 units of Stronidium since they burn at 1 unit per 6 seconds now.
UPDATE dbo.BaseStructure
SET ReinforcedStorage = 7200
WHERE ID IN (1, 2, 3)

-- Convert existing Stronidium storage to the new rate. I.E: 24 units -> 7200 units after the change.
UPDATE dbo.PCBase
SET ReinforcedFuel = ReinforcedFuel * 300
WHERE Sector IN ('SE', 'SW', 'NE', 'NW')




INSERT INTO dbo.Perk ( ID ,
                       Name ,
                       IsActive ,
                       ScriptName ,
                       BaseFPCost ,
                       BaseCastingTime ,
                       Description ,
                       PerkCategoryID ,
                       CooldownCategoryID ,
                       ExecutionTypeID ,
                       IsTargetSelfOnly ,
                       Enmity ,
                       EnmityAdjustmentRuleID ,
                       CastAnimationID )
VALUES ( 14 ,    -- ID - int
         'Stronidium Refining' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         'Engineering.StronidiumRefining' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Increases the yield of Stronidium when refining. (Refining requires the ''Refining'' perk.)' ,  -- Description - nvarchar(256)
         33 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL      -- CastAnimationID - int
    )

DECLARE @PerkLevelID INT 
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 14 , -- PerkID - int
         1 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 100% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         2 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 200% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         3 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 300% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         4 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 400% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         5 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 500% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         6 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 600% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         7 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 700% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         8 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 800% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         9 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 900% of normal.' -- Description - nvarchar(512)
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
VALUES ( 14 , -- PerkID - int
         10 , -- Level - int
         2 , -- Price - int
         N'Stronidium yield increased by 1000% of normal.' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         22 , -- SkillID - int
         50   -- RequiredRank - int
    )