UPDATE Perk
Set IsActive = 0
WHERE ID = 1;

UPDATE Perk
Set IsActive = 0
WHERE ID = 10;

UPDATE Perk
Set IsActive = 0
WHERE ID = 79;

UPDATE Perk
Set IsActive = 0
WHERE ID = 98;

UPDATE Perk
Set IsActive = 0
WHERE ID = 101;

UPDATE Perk
Set IsActive = 1
WHERE ID = 175;

Update PerkFeat
Set BaseFPCost = 4
WHERE FeatId = 1221;

Update PerkFeat
Set BaseFPCost = 5
WHERE FeatId = 1222;

Update PerkFeat
Set BaseFPCost = 6
WHERE FeatId = 1223;

Update PerkFeat
Set BaseFPCost = 8
WHERE FeatId = 1221;

Update PerkFeat
Set BaseFPCost = 10
WHERE FeatId = 1221;

Update CooldownCategory
Set BaseCooldownTime = 30
Where ID = 16;

Update PerkFeat
Set ConcentrationTickInterval = 6
Where PerkID = 175;

Update PerkFeat
Set ConcentrationFPCost = 2
Where ID = 97;

Update PerkFeat
Set ConcentrationFPCost = 3
Where ID = 98;

Update PerkFeat
Set ConcentrationFPCost = 5
Where ID = 99;