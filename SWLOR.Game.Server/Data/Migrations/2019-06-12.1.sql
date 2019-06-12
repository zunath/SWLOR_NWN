
UPDATE dbo.PerkCategory
SET Name = 'Force Alter'
WHERE ID = 40

UPDATE dbo.PerkCategory
SET Name = 'Force Control'
WHERE ID = 43

UPDATE dbo.PerkCategory
SET Name = 'Force Sense'
WHERE ID = 46

UPDATE dbo.Perk
SET PerkCategoryID = 40
WHERE PerkCategoryID IN (41, 42, 49)

UPDATE dbo.Perk
SET PerkCategoryID = 43
WHERE PerkCategoryID IN (44, 45, 50)

UPDATE dbo.Perk
SET PerkCategoryID = 46
WHERE PerkCategoryID IN (47, 48, 51)

DELETE FROM dbo.PerkCategory
WHERE ID IN (41, 42, 49, 44, 45, 50, 47, 48, 51)
