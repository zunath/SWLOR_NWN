
-- Remove message pertaining to saberstaffs in this perk.
UPDATE dbo.Perk
SET Description = 'Grants bonuses and reduces penalties while wielding twin vibroblades.'
WHERE ID = 107

-- Rename categories for sabers
UPDATE dbo.PerkCategory
SET Name = 'Lightsabers'
WHERE ID = 36

UPDATE dbo.PerkCategory
SET Name = 'Saberstaffs'
WHERE ID = 14

-- Move lightsabers to proper category
UPDATE dbo.Perk
SET PerkCategoryID = 36
WHERE ID IN (87, 105)

-- Remove dead duplicate saberstaff category
DELETE FROM dbo.PerkCategory
WHERE ID = 35

-- Disable farming perks
UPDATE dbo.Perk
SET IsActive = 0
WHERE ID IN (91, 92)

-- Move knockdown perk to the batons category
UPDATE dbo.Perk
SET PerkCategoryID = 12
WHERE ID = 50

-- Add the Saberstaff mastery perk
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
VALUES ( 153 ,    -- ID - int
         'Saberstaff Mastery' ,   -- Name - varchar(64)
         NULL ,    -- FeatID - int
         1 , -- IsActive - bit
         'Lightsaber.SaberstaffMastery' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Grants bonuses and reduces penalties while wielding saberstaffs.' ,  -- Description - nvarchar(256)
         14 ,    -- PerkCategoryID - int
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
VALUES ( 153 , -- PerkID - int
         1 , -- Level - int
         3 , -- Price - int
         N'Grants two-weapon fighting feat which reduces attack penalty from -6/-10 to -2/-6. Must be equipped with a Saberstaff.' -- Description - nvarchar(512)
    )

DECLARE @PerkLevelID int = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         14 , -- SkillID - int
         1   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 153 , -- PerkID - int
         2 , -- Level - int
         4 , -- Price - int
         N'Grants Ambidexterity feat which reduces the attack penatly fo your off-hand weapon by 4. Must be equipped with a Saberstaff.' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         14 , -- SkillID - int
         8   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 153 , -- PerkID - int
         3 , -- Level - int
         6 , -- Price - int
         N'Grants Improved two-weapon fighting which gives you a second off-hand attack at a penalty of -5 to your attack roll. Must be equipped with a Saberstaff.' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         14 , -- SkillID - int
         15   -- RequiredRank - int
    )

UPDATE dbo.Perk
SET ScriptName = 'Lightsaber.SaberstaffProficiency'
WHERE ID = 117


-- Update description on plasma cell
UPDATE dbo.Perk
SET Description = 'Your attacks have a chance to inflict additional elemental damage over time on each hit. Must be equipped with a Blaster Pistol or Blaster Rifle.',
	IsTargetSelfOnly = 0,
	ExecutionTypeID = 0
WHERE ID = 94

-- Remove plasma cell custom effect
DELETE FROM dbo.CustomEffect
WHERE ID = 21





DECLARE @PerkID INT = 121
-- Update blaster pistol BAB pricing. Still equivalent to existing 42 SP, but they get 15 max BAB instead of the 10 other weapon types do.
UPDATE dbo.PerkLevel
SET Price = 1
WHERE PerkID = @PerkID 
	AND Level IN (1, 2, 3)

UPDATE dbo.PerkLevel
SET Price = 2
WHERE PerkID = @PerkID 
	AND Level IN (4, 5, 6)

UPDATE dbo.PerkLevel
SET Price = 3
WHERE PerkID = @PerkID 
	AND Level IN (7, 8, 9)

UPDATE dbo.PerkLevel
SET Price = 4
WHERE PerkID = @PerkID 
	AND Level IN (10)

-- Update perk level skill reqs
UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 17
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 2
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 24
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 3
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 31
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 4
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 37
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 5
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 44
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 6
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 50
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 7
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 56
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 8
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 63
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 9
)


UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 70
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 10
)

-- Add perk levels 11-15

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         11 , -- Level - int
         4 , -- Price - int
         N'+11 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         76   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         12 , -- Level - int
         4 , -- Price - int
         N'+12 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         82   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         13 , -- Level - int
         4 , -- Price - int
         N'+13 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         88   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         14 , -- Level - int
         4 , -- Price - int
         N'+14 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         94   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         15 , -- Level - int
         4 , -- Price - int
         N'+15 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         100   -- RequiredRank - int
    )

-- Update blaster rifle BAB pricing. Still equivalent to existing 42 SP, but they get 15 max BAB instead of the 10 other weapon types do.
SET @PerkID = 122


-- Update blaster pistol BAB pricing. Still equivalent to existing 42 SP, but they get 15 max BAB instead of the 10 other weapon types do.
UPDATE dbo.PerkLevel
SET Price = 1
WHERE PerkID = @PerkID 
	AND Level IN (1, 2, 3)

UPDATE dbo.PerkLevel
SET Price = 2
WHERE PerkID = @PerkID 
	AND Level IN (4, 5, 6)

UPDATE dbo.PerkLevel
SET Price = 3
WHERE PerkID = @PerkID 
	AND Level IN (7, 8, 9)

UPDATE dbo.PerkLevel
SET Price = 4
WHERE PerkID = @PerkID 
	AND Level IN (10)

-- Update perk level skill reqs
UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 17
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 2
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 24
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 3
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 31
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 4
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 37
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 5
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 44
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 6
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 50
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 7
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 56
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 8
)

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 63
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 9
)


UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 70
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = @PerkID
		AND pl.Level = 10
)

-- Add perk levels 11-15

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         11 , -- Level - int
         4 , -- Price - int
         N'+11 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         76   -- RequiredRank - int
    )
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         12 , -- Level - int
         4 , -- Price - int
         N'+12 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         82   -- RequiredRank - int
    )

	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         13 , -- Level - int
         4 , -- Price - int
         N'+13 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         88   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         14 , -- Level - int
         4 , -- Price - int
         N'+14 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         94   -- RequiredRank - int
    )

	
	
INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( @PerkID , -- PerkID - int
         15 , -- Level - int
         4 , -- Price - int
         N'+15 BAB' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         5 , -- SkillID - int
         100   -- RequiredRank - int
    )



-- Rename Sword Oath to Precision Targeting and make it work only on blasters.
UPDATE dbo.Perk
SET Name = 'Precision Targeting',
	Description = 'Increases damage with blasters but reduces AC while active. Only one stance may be active at a time.',
	ScriptName = 'Stances.PrecisionTargeting'
WHERE ID = 133

UPDATE dbo.CustomEffect
SET Name = 'Precision Targeting',
	ScriptHandler = 'PrecisionTargetingEffect',
	StartMessage = 'You shift to a precision targeting stance.'
WHERE ID = 23