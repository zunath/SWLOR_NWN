
UPDATE dbo.Perk
SET Description = 'Negates the -4 penalty for using missile weapons in melee and gains +1 to attack roll and damage with ranged weapons when the target is within 15 feet.'
WHERE ID = 28

UPDATE dbo.Perk
SET Description = 'Enables you to evade a blaster shot if you meet the difficulty check. DEX modifier increases chance of evasion. Must be equipped with martial arts weapon.'
WHERE ID = 35

UPDATE dbo.Perk
SET Description = 'You receive the same number of attacks with any rifle as you would if you were using a blaster.'
WHERE ID = 53

UPDATE dbo.Perk
SET Description = 'You gain an extra attack per round while feat is active but all attacks in the round suffer a -2 penalty. Must be equipped with a blaster.'
WHERE ID = 54

UPDATE dbo.Perk
SET Description = 'You gain an extra attack per round while feat is active but all attacks in the round suffer a -2 penalty. Must be equipped with a throwing weapon.'
WHERE ID = 55

UPDATE dbo.Perk
SET Description = 'Deals extreme damage to a target on your next attack. Highest amount of damage is done from behind the target. Must be equipped with a finesse blade.'
WHERE ID = 123

UPDATE dbo.Perk
SET Description = 'Replenish FP when you damage a target with a baton weapon. Must be equipped with a baton weapon.'
WHERE ID = 125

UPDATE dbo.Perk
SET Description = 'Enables you to evade a blaster shot if you meet the difficulty check. CHA modifier increases chance of evasion. Must be equipped with either a lightsaber or saberstaff.'
WHERE ID = 154


-- Refund the Battlemage perk.
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 125


-- Remove the Battlemage perk.
DELETE FROM dbo.PerkFeat
WHERE PerkID = 125

DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel 
	WHERE PerkID = 125
)

DELETE FROM dbo.PerkLevelQuestRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel 
	WHERE PerkID = 125
)

DELETE FROM dbo.PerkLevel
WHERE PerkID = 125

DELETE FROM dbo.Perk
WHERE ID = 125
