
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
VALUES ( 38 ,    -- ID - int
         8 ,    -- SkillCategoryID - int
         N'Ugnaught' ,  -- Name - nvarchar(32)
         20 ,    -- MaxRank - int
         1 , -- IsActive - bit
         N'Ability to speak the Ugnaught language.' ,  -- Description - nvarchar(1024)
         0 ,    -- Primary - int
         0 ,    -- Secondary - int
         0 ,    -- Tertiary - int
         0   -- ContributesToSkillCap - bit
    )