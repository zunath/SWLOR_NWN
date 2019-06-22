
INSERT INTO dbo.CooldownCategory ( ID ,
                                   Name ,
                                   BaseCooldownTime )
VALUES ( 40 ,   -- ID - int
         N'Skewer' , -- Name - nvarchar(64)
         300.0   -- BaseCooldownTime - float
    )

INSERT INTO dbo.Perk ( ID ,
                       Name ,
                       IsActive ,
                       BaseCastingTime ,
                       Description ,
                       PerkCategoryID ,
                       CooldownCategoryID ,
                       ExecutionTypeID ,
                       IsTargetSelfOnly ,
                       Enmity ,
                       EnmityAdjustmentRuleID ,
                       CastAnimationID ,
                       ForceBalanceTypeID )
VALUES ( 113 ,    -- ID - int
         'Skewer' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         0.0 ,  -- BaseCastingTime - float
         N'Interrupts your target''s concentration effect. Must be equipped with a Polearm.' ,  -- Description - nvarchar(256)
         15 ,    -- PerkCategoryID - int
         40 ,    -- CooldownCategoryID - int
         2 ,    -- ExecutionTypeID - int
         1 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         NULL ,    -- CastAnimationID - int
         0      -- ForceBalanceTypeID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 113 ,   -- PerkID - int
         1 ,   -- Level - int
         4 ,   -- Price - int
         N'25% chance to interrupt' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         2 , -- SkillID - int
         15   -- RequiredRank - int
    )

INSERT INTO dbo.PerkFeat ( PerkID ,
                           FeatID ,
                           PerkLevelUnlocked ,
                           BaseFPCost ,
                           ConcentrationFPCost ,
                           ConcentrationTickInterval )
VALUES ( 113 , -- PerkID - int
         1247 , -- FeatID - int
         1 , -- PerkLevelUnlocked - int
         0 , -- BaseFPCost - int
         0 , -- ConcentrationFPCost - int
         0   -- ConcentrationTickInterval - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 113 ,   -- PerkID - int
         2 ,   -- Level - int
         4 ,   -- Price - int
         N'50% chance to interrupt' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         2 , -- SkillID - int
         30   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 113 ,   -- PerkID - int
         3 ,   -- Level - int
         5 ,   -- Price - int
         N'75% chance to interrupt' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         2 , -- SkillID - int
         50   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 113 ,   -- PerkID - int
         4 ,   -- Level - int
         6 ,   -- Price - int
         N'100% chance to interrupt' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         2 , -- SkillID - int
         80   -- RequiredRank - int
    )