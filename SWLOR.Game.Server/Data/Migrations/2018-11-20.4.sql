
UPDATE dbo.Perk
SET Name = 'Evade Blaster Fire',
	ScriptName = 'MartialArts.EvadeBlasterFire',
	Description = 'Enables you to evade a blaster shot if you meet the difficulty check. DEX modifier increases chance of evasion.'
WHERE ID = 35

UPDATE dbo.PerkLevel
SET Description = '18 second delay between evasion attempts.'
WHERE PerkID = 35

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 35 , -- PerkID - int
         2 , -- Level - int
         4 , -- Price - int
         N'12 second delay between evasion attempts.' -- Description - nvarchar(512)
    )

DECLARE @PerkLevelID INT = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         4 , -- SkillID - int
         25   -- RequiredRank - int
    )


INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description )
VALUES ( 35 , -- PerkID - int
         3 , -- Level - int
         5 , -- Price - int
         N'6 second delay between evasion attempts.' -- Description - nvarchar(512)
    )

SET @PerkLevelID = SCOPE_IDENTITY()

INSERT INTO dbo.PerkLevelSkillRequirement ( PerkLevelID ,
                                            SkillID ,
                                            RequiredRank )
VALUES ( @PerkLevelID , -- PerkLevelID - int
         4 , -- SkillID - int
         50   -- RequiredRank - int
    )

