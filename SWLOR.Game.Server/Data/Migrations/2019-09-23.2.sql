INSERT INTO dbo.Perk (ID , Name , IsActive , BaseCastingTime , Description , PerkCategoryID , CooldownCategoryID , ExecutionTypeID ,IsTargetSelfOnly ,Enmity , EnmityAdjustmentRuleID , CastAnimationID , ForceBalanceTypeID)
VALUES	(88 ,
		N'Speedy Crafting' ,
		1 ,
		0 ,
		N'Reduces the amount of time it takes to craft items. This does not stack with other Speedy Perks' ,
		4 ,
		NULL ,
		0 ,
		0 ,
		0 ,
		0 ,
		NULL ,
		0 )

INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		1 ,
		4 ,
		N'+10% Crafting Speed' ,
		0 )

INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		2 ,
		5 ,
		N'+20% Crafting Speed' ,
		0 )

		
INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		3 ,
		6 ,
		N'+30% Crafting Speed' ,
		0 )
		
INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		4 ,
		5 ,
		N'+40% Crafting Speed' ,
		0 )
		
INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		5 ,
		6 ,
		N'+50% Crafting Speed' ,
		0 )
		
INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		6 ,
		7 ,
		N'+60% Crafting Speed' ,
		0 )
		
INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		7 ,
		8 ,
		N'+70% Crafting Speed' ,
		0 )
		
INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		8 ,
		9 ,
		N'+80% Crafting Speed' ,
		0 )
		
INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		9 ,
		10 ,
		N'+90% Crafting Speed' ,
		0 )
		
INSERT INTO dbo.PerkLevel (PerkID , Level , Price , Description , SpecializationID)
VALUES (88 ,
		10,
		11 ,
		N'+99% Crafting Speed' ,
		0 )

DELETE from dbo.PerkLevelSkillRequirement Where PerklevelID IN (1737,1738,1739,1740,1741,1742,1743,1744,1745,1746)
DELETE from dbo.PerkLevel WHERE PerkID = 1
DELETE from dbo.Perk WHERE ID = 1

DELETE from dbo.PerkLevelSkillRequirement Where PerklevelID IN (2199,2200,2201,2202,2203,2204,2205,2206,2207,2208)
DELETE from dbo.PerkLevel WHERE PerkID = 6
DELETE from dbo.Perk WHERE ID = 6

DELETE from dbo.PerkLevelSkillRequirement Where PerklevelID IN (1841,1842,1843,1844,1845,1846,1847,1848,1849,1850)
DELETE from dbo.PerkLevel WHERE PerkID = 10
DELETE from dbo.Perk WHERE ID = 10

DELETE from dbo.PerkLevelSkillRequirement Where PerklevelID IN (1380,1381,1382,1383,1384,1385,1386,1387,1388,1389)
DELETE from dbo.PerkLevel WHERE PerkID = 98
DELETE from dbo.Perk WHERE ID = 98

DELETE from dbo.PerkLevelSkillRequirement Where PerklevelID IN (1390,1391,1392,1393,1394,1395,1396,1397,1398,1399)
DELETE from dbo.PerkLevel WHERE PerkID = 99
DELETE from dbo.Perk WHERE ID = 99

DELETE from dbo.PerkLevelSkillRequirement Where PerklevelID IN (1400,1401,1402,1403,1404,1405,1406,1407,1408,1409)
DELETE from dbo.PerkLevel WHERE PerkID = 100
DELETE from dbo.Perk WHERE ID = 100

DELETE from dbo.PerkLevelSkillRequirement Where PerklevelID IN (1410,1411,1412,1413,1414,1415,1416,1417,1418,1419)
DELETE from dbo.PerkLevel WHERE PerkID = 101
DELETE from dbo.Perk WHERE ID = 101