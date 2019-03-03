
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



-- Add the assembly terminal to the structure and crafting lists.
INSERT INTO dbo.BaseStructure ( ID ,
                                BaseStructureTypeID ,
                                Name ,
                                PlaceableResref ,
                                ItemResref ,
                                IsActive ,
                                Power ,
                                CPU ,
                                Durability ,
                                Storage ,
                                HasAtmosphere ,
                                ReinforcedStorage ,
                                RequiresBasePower ,
                                ResourceStorage ,
                                RetrievalRating ,
                                FuelRating ,
                                DefaultStructureModeID )
VALUES ( 11 ,    -- ID - int
         11 ,    -- BaseStructureTypeID - int
         N'Molecular Reassembly Terminal' ,  -- Name - nvarchar(64)
         N'atom_reass' ,  -- PlaceableResref - nvarchar(16)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 , -- IsActive - bit
         0.0 ,  -- Power - float
         0.0 ,  -- CPU - float
         0.0 ,  -- Durability - float
         0 ,    -- Storage - int
         0 , -- HasAtmosphere - bit
         0 ,    -- ReinforcedStorage - int
         1 , -- RequiresBasePower - bit
         0 ,    -- ResourceStorage - int
         0 ,    -- RetrievalRating - int
         0 ,    -- FuelRating - int
         0      -- DefaultStructureModeID - int
    )

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
VALUES ( 427 ,    -- ID - int
         39 ,    -- CraftCategoryID - int
         5 ,    -- BaseLevel - int
         N'Molecular Reassembly Terminal' ,  -- ItemName - nvarchar(64)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         15 ,    -- SkillID - int
         5 ,    -- CraftDeviceID - int
         2 ,    -- PerkID - int
         3 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         32 ,    -- MainComponentTypeID - int
         5 ,    -- MainMinimum - int
         43 ,    -- SecondaryComponentTypeID - int
         1 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         0 ,    -- EnhancementSlots - int
         5 ,    -- MainMaximum - int
         1 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         11      -- BaseStructureID - int
    )



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
VALUES ( 171 ,    -- ID - int
         'Molecular Reassembly Proficiency' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         'Harvesting.MolecularReassemblyProficiency' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Improves your ability to reassemble components from fully-built equipment. Requires the use of an Molecular Reassembly Terminal.' ,  -- Description - nvarchar(256)
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
VALUES ( 171 , -- PerkID - int
         1 , -- Level - int
         2 , -- Price - int
         N'+10 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         2 , -- Level - int
         2 , -- Price - int
         N'+20 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         3 , -- Level - int
         3 , -- Price - int
         N'+30 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         4 , -- Level - int
         3 , -- Price - int
         N'+40 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         5 , -- Level - int
         4 , -- Price - int
         N'+50 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         6 , -- Level - int
         4 , -- Price - int
         N'+60 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         7 , -- Level - int
         5 , -- Price - int
         N'+70 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         8 , -- Level - int
         5 , -- Price - int
         N'+80 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         9 , -- Level - int
         6 , -- Price - int
         N'+90 to property transfer' -- Description - nvarchar(512)
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
VALUES ( 171 , -- PerkID - int
         10 , -- Level - int
         7 , -- Price - int
         N'+100 to property transfer' -- Description - nvarchar(512)
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         10 , -- SkillID - int
         100   -- RequiredRank - int
    )
