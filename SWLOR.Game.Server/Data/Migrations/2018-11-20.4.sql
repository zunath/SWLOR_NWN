DECLARE @PerkLevelID INT

INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 2 ,    -- ID - int
         N'Lightsabers & Saberstaffs' ,  -- Name - nvarchar(64)
         1 , -- IsActive - bit
         2      -- Sequence - int
    )

UPDATE dbo.Perk
SET Name = 'Evade Blaster Fire',
	ScriptName = 'MartialArts.EvadeBlasterFire',
	Description = 'Enables you to evade a blaster shot if you meet the difficulty check. DEX modifier increases chance of evasion. Must be equipped with martial arts weapon and light armor.',
	ExecutionTypeID = 0
WHERE ID = 35

UPDATE dbo.PerkLevel
SET Description = '18 second delay between evasion attempts.'
WHERE PerkID = 35

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 35 , -- PerkID - int
         2 , -- Level - int
         4 , -- Price - int
         N'12 second delay between evasion attempts.' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         4 , -- SkillID - int
         25   -- RequiredRank - int
    )


INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 35 , -- PerkID - int
         3 , -- Level - int
         5 , -- Price - int
         N'6 second delay between evasion attempts.' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         4 , -- SkillID - int
         50   -- RequiredRank - int
    )



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
VALUES ( 154 ,    -- ID - int
         'Deflect Blaster Fire' ,   -- Name - varchar(64)
         NULl ,    -- FeatID - int
         1 , -- IsActive - bit
         'Lightsaber.DeflectBlasterFire' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Enables you to evade a blaster shot if you meet the difficulty check. CHA modifier increases chance of evasion. Must be equipped with force armor and either a lightsaber or saberstaff.' ,
         2 ,    -- PerkCategoryID - int
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
VALUES ( 154 , -- PerkID - int
         1 , -- Level - int
         2 , -- Price - int
         N'18 second delay between deflection attempts.' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         14 , -- SkillID - int
         10   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 154 , -- PerkID - int
         2 , -- Level - int
         4 , -- Price - int
         N'12 second delay between deflection attempts.' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         14 , -- SkillID - int
         25   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 154 , -- PerkID - int
         3 , -- Level - int
         5 , -- Price - int
         N'6 second delay between deflection attempts.' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         14 , -- SkillID - int
         50   -- RequiredRank - int
    )
