
DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)
)

DELETE FROM dbo.PerkLevelQuestRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)
)

DELETE FROM dbo.PCPerkRefund
WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)

DELETE FROM dbo.PerkLevel
WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)

DELETE FROM dbo.Perk
WHERE ID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)




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
VALUES ( 38 ,    -- ID - int
         'Guild Relations' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         0.0 ,  -- BaseCastingTime - float
         N'Improves your GP acquisition with all guilds.' ,  -- Description - nvarchar(256)
         4 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         0 ,    -- CastAnimationID - int
         0      -- ForceBalanceTypeID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         1 ,   -- Level - int
         6 ,   -- Price - int
         N'Doubles GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    ) 

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         2 ,   -- Level - int
         6 ,   -- Price - int
         N'Triples GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         3 ,   -- Level - int
         6 ,   -- Price - int
         N'Quadruples GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )
