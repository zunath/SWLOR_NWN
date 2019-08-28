
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
VALUES ( 11 ,    -- ID - int
         'Martial Finesse' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         0.0 ,  -- BaseCastingTime - float
         N'Grants the Weapon Finesse feat which enables you to make attack rolls with your dexterity modifier instead of strength, if it is higher. Requires a martial arts weapon or unarmed to use.' ,  -- Description - nvarchar(256)
         17 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         5 ,    -- ExecutionTypeID - int
         0 , -- IsTargetSelfOnly - bit
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
VALUES ( 11 ,   -- PerkID - int
         1 ,   -- Level - int
         3 ,   -- Price - int
         N'Grants the Weapon Finesse feat when equipped with a martial arts weapon or unarmed.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( SCOPE_IDENTITY() , -- PerkLevelID - int
         4 , -- SkillID - int
         1   -- RequiredRank - int
    )