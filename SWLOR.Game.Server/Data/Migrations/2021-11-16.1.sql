-- Refund all Finesse perks
CALL swlor.ADM_RefundPlayerPerk(11);
CALL swlor.ADM_RefundPlayerPerk(56);
CALL swlor.ADM_RefundPlayerPerk(160);

-- Deactivate Martial Finesse and Saber Finesse
UPDATE Perk 
SET IsActive = 0
WHERE ID IN (11, 160);

-- Move Weapon Finesse to General category
UPDATE Perk
SET PerkCategoryID = 4
WHERE ID = 56;

-- Remove skill requirement from Weapon Finesse perk.
DELETE FROM PerkLevelSkillRequirement
WHERE PerkLevelID IN(
	SELECT ID
    FROM PerkLevel 
    WHERE PerkID = 56
);