INSERT INTO dbo.CustomEffects (CustomEffectID ,
					Name ,
					IconID ,
					ScriptHandler ,
					StartMessage ,
					ContinueMessage ,
					WornOffMessage ,
					IsStance
				)

VALUES (25 , -- CustomEffectID 
		N'Force Leech' ,	--	Name 
		0 ,	--	IconID 
		N'ForceLeechEffect' ,	--	ScriptHandler 
		N'You feel your life leaving you' ,	--	StartMessage 
		N'You continue to grow weaker' ,	--	ContinueMessage 
		N'You are starting to feel normal again' ,	--	WornOffMessage 
		0 	--	IsStance
	)
	
	
	
	
	
	INSERT INTO dbo.Perks( PerkID ,
                         Name ,
                         FeatID ,
                         IsActive ,
						 ScriptName,
						 BaseFPCost,
						 BaseCastingTime,
                         Description ,
						 PerkCategoryID,
						 CooldownCategoryID,
						 ExecutionTypeID,
						 ItemResref,
						 IsTargetSelfOnly,
						 Enmity,
						 EnmityAdjustmentRuleID,
						 CastAnimationID 
						 )

VALUES ( 152 ,    -- PerkID - int
         N'Force Leech' ,  -- Name - nvarchar(32)
         1162 ,    -- FeatID - int
		 1 , -- IsActive - bit
		 N'DarkSide.ForceLeech' ,  -- ScriptName
		 8 ,    -- BaseFPCost
		 0 ,    -- BaseCastingTime 
		 N'Inflicts your target with energy to sap their life, bringing a portion back to yourself.' ,  -- Description - nvarchar(1024)
		 29 ,    -- PerkCategoryID
		 6 ,    -- CooldownCategoryID
		 3 ,    -- ExecutionTypeID
		 NULL , -- ItemResref
		 0 ,    -- IsTargetSelf Only
		 10 ,    -- Enmity
		 1 ,    -- EnmityAdjustmentRuleID
		 NULL     -- CastAnimationID 
	 )

INSERT INTO dbo.PerkLevels (
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		1 , --Level
		3 , --Price
		N'Damage 2, Recovery 50%, 6 ticks'  --Description
    )

INSERT INTO dbo.PerkLevels(
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		2 , --Level
		4 , --Price
		N'Damage 2, Recovery 50%, 9 ticks.'  --Description
    )

INSERT INTO dbo.PerkLevels ( 
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		3 , --Level
		5 , --Price
		N'Damage 4, Recovery 50%, 6 ticks.'  --Description
    )

INSERT INTO dbo.PerkLevels ( 
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		4 , --Level
		6 , --Price
		N'Damage 4, Recovery 75%, 6ticks.'  --Description
    )

INSERT INTO dbo.PerkLevels ( 
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		5 , --Level
		7 , --Price
		N'Damage 4, Recovery 75%, 9 ticks.'  --Description
    )

INSERT INTO dbo.PerkLevels (
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		6 , --Level
		8 , --Price
		N'Damage 6, Recovery 75%, 9ticks.'  --Description
    )

INSERT INTO dbo.PerkLevels (
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		7 , --Level
		9 , --Price
		N'Damage 8, Recovery 50%, 12 ticks.'  --Description
    )

INSERT INTO dbo.PerkLevels (
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		8 , --Level
		10 , --Price
		N'Damage 8, Recovery 75%, 12 ticks.'  --Description
    )

INSERT INTO dbo.PerkLevels (
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		9 , --Level
		11 , --Price
		N'Damage 12, Recovery 50%, 15 ticks.'  --Description
    )

INSERT INTO dbo.PerkLevels(
							PerkID ,
							Level ,
							Price , 
							Description
 )
VALUES (
		152 , --PerkID
		10 , --Level
		12 , --Price
		N'Damage 12, Recovery 75%, 15 ticks.'  --Description
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2080 , --PerkLevelID
		29 , --SkillID
		0  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2081 , --PerkLevelID
		29 , --SkillID
		10  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2082 , --PerkLevelID
		29 , --SkillID
		20  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2083 , --PerkLevelID
		29 , --SkillID
		30  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2084 , --PerkLevelID
		29 , --SkillID
		40  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2085 , --PerkLevelID
		29 , --SkillID
		50  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2086 , --PerkLevelID
		29 , --SkillID
		60  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2087 , --PerkLevelID
		29 , --SkillID
		70  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2088 , --PerkLevelID
		29 , --SkillID
		80  --RequiredRank
    )

INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2089 , --PerkLevelID
		29 , --SkillID
		90  --RequiredRank
    )

	INSERT INTO dbo.PerkLevelSkillRequirements (
                            PerkLevelID ,
                            SkillID ,
                            RequiredRank                           
 )

VALUES (
		2090 , --PerkLevelID
		29 , --SkillID
		100  --RequiredRank
    )