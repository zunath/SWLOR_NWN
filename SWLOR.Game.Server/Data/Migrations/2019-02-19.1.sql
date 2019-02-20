
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
VALUES ( 37 ,    -- ID - int
         8 ,    -- SkillCategoryID - int
         N'Mon Calamarian' ,  -- Name - nvarchar(32)
         20 ,    -- MaxRank - int
         1 , -- IsActive - bit
         N'Ability to speak the Mon Calamarian language.' ,  -- Description - nvarchar(1024)
         0 ,    -- Primary - int
         0 ,    -- Secondary - int
         0 ,    -- Tertiary - int
         0   -- ContributesToSkillCap - bit
    )

INSERT INTO dbo.SkillXPRequirement ( SkillID ,
                                     Rank ,
                                     XP )
SELECT 37 ,
       Rank ,
       XP 
FROM dbo.SkillXPRequirement 
WHERE SkillID = 1
