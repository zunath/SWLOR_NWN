
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 106 -- int

DELETE FROM dbo.PCPerkRefund
WHERE PerkID = 106

DELETE FROM dbo.PerkFeat
WHERE PerkID = 106

DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel 
	WHERE PerkID = 106
)

DELETE FROM dbo.PerkLevelQuestRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel 
	WHERE PerkID = 106
)

DELETE FROM dbo.PerkLevel
WHERE PerkID = 106

DELETE FROM dbo.Perk
WHERE ID = 106
