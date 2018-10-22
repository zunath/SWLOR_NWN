
UPDATE dbo.Skills
SET Name = 'Droidspeak'
WHERE SkillID = 32

INSERT INTO dbo.Skills ( SkillID ,
                         SkillCategoryID ,
                         Name ,
                         MaxRank ,
                         IsActive ,
                         Description ,
                         [Primary] ,
                         Secondary ,
                         Tertiary ,
                         ContributesToSkillCap )
VALUES ( 33 ,    -- SkillID - int
         8 ,    -- SkillCategoryID - int
         N'Basic' ,  -- Name - nvarchar(32)
         20 ,    -- MaxRank - int
         1 , -- IsActive - bit
         N'Ability to speak the Galactic Basic language.' ,  -- Description - nvarchar(1024)
         0 ,    -- Primary - int
         0 ,    -- Secondary - int
         0 ,    -- Tertiary - int
         0   -- ContributesToSkillCap - bit
    )

INSERT INTO dbo.SkillXPRequirement ( SkillID ,
                                     Rank ,
                                     XP )
SELECT 33,
	Rank,
	XP 
FROM dbo.SkillXPRequirement 
WHERE SkillID = 1

INSERT INTO dbo.Skills ( SkillID ,
                         SkillCategoryID ,
                         Name ,
                         MaxRank ,
                         IsActive ,
                         Description ,
                         [Primary] ,
                         Secondary ,
                         Tertiary ,
                         ContributesToSkillCap )
VALUES ( 34 ,    -- SkillID - int
         8 ,    -- SkillCategoryID - int
         N'Mandoa' ,  -- Name - nvarchar(32)
         20 ,    -- MaxRank - int
         1 , -- IsActive - bit
         N'Ability to speak the Mandoa language.' ,  -- Description - nvarchar(1024)
         0 ,    -- Primary - int
         0 ,    -- Secondary - int
         0 ,    -- Tertiary - int
         0   -- ContributesToSkillCap - bit
    )

INSERT INTO dbo.SkillXPRequirement ( SkillID ,
                                     Rank ,
                                     XP )
SELECT 34,
	Rank,
	XP 
FROM dbo.SkillXPRequirement 
WHERE SkillID = 1


INSERT INTO dbo.Skills ( SkillID ,
                         SkillCategoryID ,
                         Name ,
                         MaxRank ,
                         IsActive ,
                         Description ,
                         [Primary] ,
                         Secondary ,
                         Tertiary ,
                         ContributesToSkillCap )
VALUES ( 35 ,    -- SkillID - int
         8 ,    -- SkillCategoryID - int
         N'Huttese' ,  -- Name - nvarchar(32)
         20 ,    -- MaxRank - int
         1 , -- IsActive - bit
         N'Ability to speak the Huttese language.' ,  -- Description - nvarchar(1024)
         0 ,    -- Primary - int
         0 ,    -- Secondary - int
         0 ,    -- Tertiary - int
         0   -- ContributesToSkillCap - bit
    )

INSERT INTO dbo.SkillXPRequirement ( SkillID ,
                                     Rank ,
                                     XP )
SELECT 35,
	Rank,
	XP 
FROM dbo.SkillXPRequirement 
WHERE SkillID = 1
