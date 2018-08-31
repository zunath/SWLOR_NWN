

BEGIN TRANSACTION

DECLARE @PerkID INT = (SELECT MAX(PerkID) + 1 FROM dbo.Perks)
DECLARE @Name NVARCHAR(64) = 'Careful Forager'
DECLARE @PerkDescription NVARCHAR(256) = N'Reduces the chance of exhausting a resource.'
DECLARE @PerkJS NVARCHAR(64) = 'Gathering.CarefulForager'
DECLARE @SkillID INT = 24
DECLARE @PerkCategoryID INT = 3

INSERT INTO dbo.Perks ( PerkID ,
                        Name ,
                        FeatID ,
                        IsActive ,
                        JavaScriptName ,
                        BaseManaCost ,
                        BaseCastingTime ,
                        Description ,
                        PerkCategoryID ,
                        CooldownCategoryID ,
                        ExecutionTypeID ,
                        ItemResref ,
                        IsTargetSelfOnly )
VALUES ( @PerkID ,    -- PerkID - int
         @Name ,   -- Name - varchar(64)
         NULL ,    -- FeatID - int
         1 , -- IsActive - bit
         @PerkJS ,   -- JavaScriptName - varchar(64)
         0 ,    -- BaseManaCost - int
         0 ,  -- BaseCastingTime - float
         @PerkDescription,
         @PerkCategoryID ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         NULL  ,  -- ItemResref - nvarchar(16)
         0   -- IsTargetSelfOnly - bit
    )

DECLARE @PerkLevelID INT
DECLARE @PerkLevel INT = 1

INSERT INTO dbo.PerkLevels ( PerkID ,
                             Level ,
                             Price ,
                             Description )
VALUES ( @PerkID , -- PerkID - int
         @PerkLevel , -- Level - int
         1 , -- Price - int
         '-5% Chance'
    )

SET @PerkLevelID = SCOPE_IDENTITY()
SET @PerkLevel = @PerkLevel + 1

INSERT INTO dbo.PerkLevelSkillRequirements ( PerkLevelID ,
                                             SkillID ,
                                             RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         @SkillID , -- SkillID - int
         5   -- RequiredRank - int
    )




INSERT INTO dbo.PerkLevels ( PerkID ,
                             Level ,
                             Price ,
                             Description )
VALUES ( @PerkID , -- PerkID - int
         @PerkLevel , -- Level - int
         1 , -- Price - int
         '-10% Chance'
    )

SET @PerkLevelID = SCOPE_IDENTITY()
SET @PerkLevel = @PerkLevel + 1

INSERT INTO dbo.PerkLevelSkillRequirements ( PerkLevelID ,
                                             SkillID ,
                                             RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         @SkillID , -- SkillID - int
         10   -- RequiredRank - int
    )



INSERT INTO dbo.PerkLevels ( PerkID ,
                             Level ,
                             Price ,
                             Description )
VALUES ( @PerkID , -- PerkID - int
         @PerkLevel , -- Level - int
         2 , -- Price - int
         '-15% Chance'
    )

SET @PerkLevelID = SCOPE_IDENTITY()
SET @PerkLevel = @PerkLevel + 1

INSERT INTO dbo.PerkLevelSkillRequirements ( PerkLevelID ,
                                             SkillID ,
                                             RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         @SkillID , -- SkillID - int
         15   -- RequiredRank - int
    )




INSERT INTO dbo.PerkLevels ( PerkID ,
                             Level ,
                             Price ,
                             Description )
VALUES ( @PerkID , -- PerkID - int
         @PerkLevel , -- Level - int
         3 , -- Price - int
         '-20% Chance'
    )

SET @PerkLevelID = SCOPE_IDENTITY()
SET @PerkLevel = @PerkLevel + 1

INSERT INTO dbo.PerkLevelSkillRequirements ( PerkLevelID ,
                                             SkillID ,
                                             RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         @SkillID , -- SkillID - int
         20   -- RequiredRank - int
    )



INSERT INTO dbo.PerkLevels ( PerkID ,
                             Level ,
                             Price ,
                             Description )
VALUES ( @PerkID , -- PerkID - int
         @PerkLevel , -- Level - int
         4 , -- Price - int
         '-25% Chance'
    )

SET @PerkLevelID = SCOPE_IDENTITY()
SET @PerkLevel = @PerkLevel + 1

INSERT INTO dbo.PerkLevelSkillRequirements ( PerkLevelID ,
                                             SkillID ,
                                             RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         @SkillID , -- SkillID - int
         25   -- RequiredRank - int
    )



INSERT INTO dbo.PerkLevels ( PerkID ,
                             Level ,
                             Price ,
                             Description )
VALUES ( @PerkID , -- PerkID - int
         @PerkLevel , -- Level - int
         4 , -- Price - int
         '-30% Chance'
    )

SET @PerkLevelID = SCOPE_IDENTITY()
SET @PerkLevel = @PerkLevel + 1

INSERT INTO dbo.PerkLevelSkillRequirements ( PerkLevelID ,
                                             SkillID ,
                                             RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         @SkillID , -- SkillID - int
         30   -- RequiredRank - int
    )







-- rollback
-- commit
