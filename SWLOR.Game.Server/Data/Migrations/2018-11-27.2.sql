
-- Reduce price on optimization and efficiency perks to 7 SP
UPDATE dbo.PerkLevel
SET Price = 7
WHERE PerkID IN (
137,138,139,140,141,143,144,145,146,147) 

-- Reduce level 1 of optimization and efficiency perks to 10
UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 10
WHERE PerkLevelID IN (
	SELECT pl.ID
	FROM dbo.PerkLevel pl
	WHERE pl.Level = 1
		AND pl.PerkID IN ( 137,138,139,140,141,143,144,145,146,147) 
)

-- Reduce level 2 of optimization and efficiency perks to 20
UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 20
WHERE PerkLevelID IN (
	SELECT pl.ID
	FROM dbo.PerkLevel pl
	WHERE pl.Level = 2
		AND pl.PerkID IN ( 137,138,139,140,141,143,144,145,146,147) 
)

-- Reduce construction parts base level and reduce required amount of metal
UPDATE dbo.CraftBlueprint
SET BaseLevel = 0,
	MainMinimum = 2
WHERE ID = 319

-- Reduce all blades to level 1 base
UPDATE dbo.CraftBlueprint
SET MainMinimum = 1
WHERE ID IN (92,93)

-- Reduce power core base level to 0
UPDATE dbo.CraftBlueprint
SET BaseLevel = 0
WHERE ID = 321

-- Add Mynock Tooth to Mynock CZ-220 loot table.
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 12 ,    -- LootTableID - int
         'mynock_tooth' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         12 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )


-- Update existing dual wielding perk name and description
UPDATE dbo.Perk
SET Name = 'One-Handed Dual Wielding',
	Description = 'Grants bonuses while wielding two weapons. Must be equipped with a non-lightsaber one-handed weapon.'
WHERE ID = 29

UPDATE dbo.PerkLevel
SET Description = REPLACE(Description, 'Must be equipped with a one-handed weapon.', 'Must be equipped with a one-handed non-lightsaber weapon')
WHERE PerkID = 29


-- Add new lightsaber dual wielding perk
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
VALUES (  155,    -- ID - int
         'Lightsaber Dual Wielding' ,   -- Name - varchar(64)
         NULL ,    -- FeatID - int
         1 , -- IsActive - bit
         'Lightsaber.DualWielding' ,   -- ScriptName - varchar(64)
         0 ,    -- BaseFPCost - int
         0.0 ,  -- BaseCastingTime - float
         N'Grants bonuses while wielding two weapons. Must be equipped with a one-handed lightsaber.' ,  -- Description - nvarchar(256)
         36 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         5 ,    -- ExecutionTypeID - int
         N'' ,  -- ItemResref - nvarchar(16)
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
VALUES ( 155 , -- PerkID - int
         1 , -- Level - int
         3 , -- Price - int
         N'Grants Two-weapon Fighting feat which reduces attack penalty from -6/-10 to -4/-8 when fighting with two weapons. Must be equipped with a one-handed lightsaber.' -- Description - nvarchar(512)
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
VALUES ( 155 , -- PerkID - int
         2 , -- Level - int
         4 , -- Price - int
         N'Grants Ambidexterity feat which reduces the attack penalty of your off-hand weapon by 4. Must be equipped with a one-handed lightsaber.'
    )
SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         14 , -- SkillID - int
         15   -- RequiredRank - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 155 , -- PerkID - int
         3 , -- Level - int
         6 , -- Price - int
         N'Grants Improved two-weapon fighting which gives you a second off-hand attack at a penalty of -5 to your attack roll. Must be equipped with a one-handed lightsaber.'
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         14 , -- SkillID - int
         25   -- RequiredRank - int
    )


-- Fix description for website topic
UPDATE dbo.GameTopic
SET Text = 'This is a time of despair.\n\nAfter more than a decade of conflict, the Mandalorian Wars have finally ended. The Mandalorian clans are now scattered across the Outer Rim. They patiently wait for the right time to reunite and rise again.\n\nThis is a time of uncertainty.\n\nMillions died during the conflict and the Republic has begun to rebuild. Their stability is threatened by the numerous criminal groups and bounty hunters who aim to extend their control throughout the Outer Rim. The Republic struggles to preserve its order.\n\nThis is a time of disturbance.\n\nThe Sith Empire, secretly involved in the Mandalorian Wars, has risen again. Darth Revan and Darth Malak, former members of the Jedi Order, have turned to the Dark Side. With the help of the Star Forge, an automated factory and battle dreadnought, they have assembled a powerful army and taken control of Korriban. The Sith aim to conquer the galaxy.\n\nThis is a time of choice.\n\nWill you join the Jedi Order and attempt to preserve peace and balance in the galaxy? Or will you side with the Sith and harness the powers of the Dark Side? Perhaps you''ll play both sides as a smuggler working with the Hutt Cartel? Or maybe you''ll work as a freelancer, serving no one but yourself.\n\nIt''s time to make a choice and navigate your own way through the galaxy...'
WHERE ID = 62