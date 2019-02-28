
ALTER TABLE dbo.ComponentType
ADD ReassembledResref NVARCHAR(16) NOT NULL DEFAULT ''

GO



UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_metal'
WHERE ID = 2

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_organic'
WHERE ID = 3

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_fiberplast'
WHERE ID = 12

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_leather'
WHERE ID = 13

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_electronics'
WHERE ID = 15

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_powcry'
WHERE ID = 21


UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_bluecry'
WHERE ID = 24

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_redcry'
WHERE ID = 25

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_greencry'
WHERE ID = 26

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_yellowcry'
WHERE ID = 27

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_herb'
WHERE ID = 48




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
VALUES ( 6 ,    -- ID - int
         'Speedy Reassembly' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         'Harvesting.SpeedyReassembly' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Reduces time it takes to reassemble an item.' ,  -- Description - nvarchar(256)
         34 ,    -- PerkCategoryID - int
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
VALUES ( 6 , -- PerkID - int
         1 , -- Level - int
         2 , -- Price - int
         N'+10% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         10   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         2 , -- Level - int
         2 , -- Price - int
         N'+10% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         20   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         3 , -- Level - int
         3 , -- Price - int
         N'+30% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         30   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         4 , -- Level - int
         3 , -- Price - int
         N'+40% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         40   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         5 , -- Level - int
         4 , -- Price - int
         N'+50% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         50   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         6 , -- Level - int
         4 , -- Price - int
         N'+60% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         60   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         7 , -- Level - int
         5 , -- Price - int
         N'+70% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         70   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         8 , -- Level - int
         5 , -- Price - int
         N'+80% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         80   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         9 , -- Level - int
         6 , -- Price - int
         N'+90% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         90   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 6 , -- PerkID - int
         10 , -- Level - int
         7 , -- Price - int
         N'+99% Speed' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         100   -- RequiredRank - int
    )
