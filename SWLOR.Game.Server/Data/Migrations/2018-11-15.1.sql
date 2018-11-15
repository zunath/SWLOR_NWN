
UPDATE dbo.Skill
SET Description = 'Ability to use one-handed weapons like vibroblades and batons. Higher skill levels unlock more powerful one-handed weapons.'
WHERE ID = 1

INSERT INTO dbo.Skill ( ID ,
                        SkillCategoryID ,
                        Name ,
                        MaxRank ,
                        IsActive ,
                        Description ,
                        [Primary] ,
                        Secondary ,
                        Tertiary ,
                        ContributesToSkillCap )
VALUES ( 14 ,    -- ID - int
         1 ,    -- SkillCategoryID - int
         N'Lightsabers' ,  -- Name - nvarchar(32)
         100 ,    -- MaxRank - int
         1 , -- IsActive - bit
         N'Ability to use lightsaber and saberstaff weapons. Higher skill levels unlock more powerful lightsabers and saberstaff weapons.' ,  -- Description - nvarchar(1024)
         2 ,    -- Primary - int
         6 ,    -- Secondary - int
         0 ,    -- Tertiary - int
         1   -- ContributesToSkillCap - bit
    )


INSERT INTO dbo.SkillXPRequirement ( SkillID ,
                                     Rank ,
                                     XP )
SELECT 14 ,
       Rank ,
       XP 
FROM dbo.SkillXPRequirement
WHERE SkillID = 1 


UPDATE dbo.PerkLevelSkillRequirement
SET SkillID = 14
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID IN (87, 105, 129) 
)

UPDATE dbo.Perk
SET ScriptName = 'Lightsaber.LightsaberProficiency'
WHERE ID = 129
