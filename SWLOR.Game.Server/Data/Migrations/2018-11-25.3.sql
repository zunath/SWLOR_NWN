
UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = 1
WHERE ID = 558
UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = 3
WHERE ID = 559
UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = 5
WHERE ID = 560
UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = 7
WHERE ID = 561


UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = 1
WHERE ID = 570
UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = 3
WHERE ID = 571
UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = 5
WHERE ID = 572
UPDATE dbo.CraftBlueprint
SET RequiredPerkLevel = 7
WHERE ID = 573




-- Reduce all repair kits by 5 levels.
UPDATE dbo.CraftBlueprint
SET BaseLevel = BaseLevel - 5
WHERE ID IN (
538,539,540,541,542,543,544,545,546,547,548,549,550,551,552,553,554,555,556,557,
558,559,560,561,562,563,564,565,566,567,568,569,570,571,572,573,574,575,576,577,
578,579,580,581,582,583,584,585,586,587,588,589,590,591,592,593,594,595,596,597)
