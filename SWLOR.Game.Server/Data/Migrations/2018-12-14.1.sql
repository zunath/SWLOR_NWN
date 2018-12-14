
-- =============================================
-- Author:		zunath
-- Create date: 2018-12-14
-- Description:	Removes a specific perk from all players and refunds the SP they spent on it.
-- =============================================
CREATE PROCEDURE ADM_RefundPlayerPerk
	@PerkID INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @PlayerSPRefunds AS TABLE(
		PlayerID UNIQUEIDENTIFIER,
		RefundedSP INT
	)

	INSERT INTO @PlayerSPRefunds ( PlayerID ,
	                               RefundedSP )
	SELECT pcp.PlayerID,
		SUM(pl.Price) 
	FROM dbo.PCPerk pcp
	JOIN dbo.PerkLevel pl ON pl.PerkID = pcp.PerkID AND pl.Level <= pcp.PerkLevel
	WHERE pcp.PerkID = @PerkID
	GROUP BY pcp.PlayerID

	UPDATE dbo.Player
	SET UnallocatedSP = UnallocatedSP + sp.RefundedSP
	FROM @PlayerSPRefunds sp
	WHERE dbo.Player.ID = sp.PlayerID

	DELETE FROM dbo.PCPerk
	WHERE ID IN (
		SELECT pcp.ID
		FROM dbo.PCPerk pcp
		JOIN @PlayerSPRefunds pspr ON pspr.PlayerID = pcp.PlayerID
		WHERE pcp.PerkID = @PerkID  
	)

END
GO


-- Refund Force Breach
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 3 -- int

-- Refund Force Lightning
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 4 -- int

-- Refund Force Heal
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 5 -- int

-- Refund Dark Heal
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 6 -- int

-- Refund Force Spread
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 13 -- int

-- Refund Dark Spread
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 14 -- int

-- Refund Force Push
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 19 -- int

-- Refund Force Aura
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 76 -- int

-- Refund Drain Life
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 78 -- int

-- Refund Chainspell
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 126 -- int

GO


-- Convert Dark Side to Force Combat
UPDATE dbo.Skill
SET Name = 'Force Combat',
	Description = 'Ability to use combat-based force abilities like Force Push and Force Lightning. Higher skill levels unlock new abilities.'
WHERE ID = 19

-- Convert Light Side to Force Support
UPDATE dbo.Skill
SET Name = 'Force Support',
	Description = 'Ability to use support-based force abilities like Force Heal and Force Aura. Higher skill levels unlock new abilities.'
WHERE ID = 20

-- Add the Force Utility skill
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
VALUES ( 21 ,    -- ID - int
         6 ,    -- SkillCategoryID - int
         N'Force Utility' ,  -- Name - nvarchar(32)
         100 ,    -- MaxRank - int
         1 , -- IsActive - bit
         N'Ability to use utility-based force abilities like Force Spread and Chainspell. Higher skill levels unlock new abilities.' ,  -- Description - nvarchar(1024)
         6 ,    -- Primary - int
         3 ,    -- Secondary - int
         2 ,    -- Tertiary - int
         1   -- ContributesToSkillCap - bit
    )

INSERT INTO dbo.SkillXPRequirement ( SkillID ,
                                     Rank ,
                                     XP )
SELECT 21,
	Rank,
	XP	 
FROM dbo.SkillXPRequirement
WHERE SkillID = 19 




UPDATE dbo.PerkCategory
SET Name = 'Force Combat'
WHERE ID = 29

UPDATE dbo.PerkCategory
SET Name = 'Force Support'
WHERE ID = 30

INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 31 ,    -- ID - int
         N'Force Utility' ,  -- Name - nvarchar(64)
         1 , -- IsActive - bit
         30      -- Sequence - int
    )

UPDATE dbo.Perk
SET ScriptName = 'ForceCombat.ForceBreach',
	PerkCategoryID = 29
WHERE ID = 3

UPDATE dbo.Perk
SET ScriptName = 'ForceCombat.ForceLightning',
	PerkCategoryID = 29
WHERE ID = 4

UPDATE dbo.Perk
SET ScriptName = 'ForceSupport.ForceHeal',
	PerkCategoryID = 30
WHERE ID = 5

UPDATE dbo.Perk
SET ScriptName = 'ForceSupport.DarkHeal',
	PerkCategoryID = 30
WHERE ID = 6

UPDATE dbo.Perk
SET ScriptName = 'ForceUtility.ForceSpread',
	PerkCategoryID = 31
WHERE ID = 13

UPDATE dbo.Perk
SET ScriptName = 'ForceUtility.DarkSpread',
	PerkCategoryID = 31
WHERE ID = 14

UPDATE dbo.Perk
SET ScriptName = 'ForceCombat.ForcePush',
	PerkCategoryID = 29
WHERE ID = 19

UPDATE dbo.Perk
SET ScriptName = 'ForceSupport.ForceAura',
	PerkCategoryID = 30
WHERE ID = 76

UPDATE dbo.Perk
SET ScriptName = 'ForceCombat.DrainLife',
	PerkCategoryID = 29
WHERE ID = 78

UPDATE dbo.Perk
SET ScriptName = 'ForceUtility.Chainspell',
	PerkCategoryID = 31
WHERE ID = 126


UPDATE dbo.Mod
SET Name = 'Force Support'
WHERE ID = 7

UPDATE dbo.Mod
SET Name = 'Force Combat'
WHERE ID = 8

UPDATE dbo.Mod
SET Name = 'Force Utility',
	Script = 'ForceUtilityMod'
WHERE ID = 9




UPDATE dbo.CraftBlueprint
SET ItemName = 'Force Support I'
WHERE ID = 128

UPDATE dbo.CraftBlueprint
SET ItemName = 'Force Support II'
WHERE ID = 161

UPDATE dbo.CraftBlueprint
SET ItemName = 'Force Support III'
WHERE ID = 191


UPDATE dbo.CraftBlueprint
SET ItemName = 'Force Combat I'
WHERE ID = 129

UPDATE dbo.CraftBlueprint
SET ItemName = 'Force Combat II'
WHERE ID = 162

UPDATE dbo.CraftBlueprint
SET ItemName = 'Force Combat III'
WHERE ID = 192

INSERT INTO dbo.CraftBlueprint ( ID ,
                                 CraftCategoryID ,
                                 BaseLevel ,
                                 ItemName ,
                                 ItemResref ,
                                 Quantity ,
                                 SkillID ,
                                 CraftDeviceID ,
                                 PerkID ,
                                 RequiredPerkLevel ,
                                 IsActive ,
                                 MainComponentTypeID ,
                                 MainMinimum ,
                                 SecondaryComponentTypeID ,
                                 SecondaryMinimum ,
                                 TertiaryComponentTypeID ,
                                 TertiaryMinimum ,
                                 EnhancementSlots ,
                                 MainMaximum ,
                                 SecondaryMaximum ,
                                 TertiaryMaximum ,
                                 BaseStructureID )
VALUES ( 132 ,    -- ID - int
         15 ,    -- CraftCategoryID - int
         15 ,    -- BaseLevel - int
         N'Force Utility I' ,  -- ItemName - nvarchar(64)
         N'rune_sum1' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         22 ,    -- SkillID - int
         4 ,    -- CraftDeviceID - int
         96 ,    -- PerkID - int
         1 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         28 ,    -- MainComponentTypeID - int
         1 ,    -- MainMinimum - int
         0 ,    -- SecondaryComponentTypeID - int
         0 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         0 ,    -- EnhancementSlots - int
         2 ,    -- MainMaximum - int
         0 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         NULL      -- BaseStructureID - int
    )

	
INSERT INTO dbo.CraftBlueprint ( ID ,
                                 CraftCategoryID ,
                                 BaseLevel ,
                                 ItemName ,
                                 ItemResref ,
                                 Quantity ,
                                 SkillID ,
                                 CraftDeviceID ,
                                 PerkID ,
                                 RequiredPerkLevel ,
                                 IsActive ,
                                 MainComponentTypeID ,
                                 MainMinimum ,
                                 SecondaryComponentTypeID ,
                                 SecondaryMinimum ,
                                 TertiaryComponentTypeID ,
                                 TertiaryMinimum ,
                                 EnhancementSlots ,
                                 MainMaximum ,
                                 SecondaryMaximum ,
                                 TertiaryMaximum ,
                                 BaseStructureID )
VALUES ( 165 ,    -- ID - int
         15 ,    -- CraftCategoryID - int
         30 ,    -- BaseLevel - int
         N'Force Utility II' ,  -- ItemName - nvarchar(64)
         N'rune_sum2' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         22 ,    -- SkillID - int
         4 ,    -- CraftDeviceID - int
         96 ,    -- PerkID - int
         5 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         28 ,    -- MainComponentTypeID - int
         3 ,    -- MainMinimum - int
         0 ,    -- SecondaryComponentTypeID - int
         0 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         0 ,    -- EnhancementSlots - int
         4 ,    -- MainMaximum - int
         0 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         NULL      -- BaseStructureID - int
    )

	
INSERT INTO dbo.CraftBlueprint ( ID ,
                                 CraftCategoryID ,
                                 BaseLevel ,
                                 ItemName ,
                                 ItemResref ,
                                 Quantity ,
                                 SkillID ,
                                 CraftDeviceID ,
                                 PerkID ,
                                 RequiredPerkLevel ,
                                 IsActive ,
                                 MainComponentTypeID ,
                                 MainMinimum ,
                                 SecondaryComponentTypeID ,
                                 SecondaryMinimum ,
                                 TertiaryComponentTypeID ,
                                 TertiaryMinimum ,
                                 EnhancementSlots ,
                                 MainMaximum ,
                                 SecondaryMaximum ,
                                 TertiaryMaximum ,
                                 BaseStructureID )
VALUES ( 180 ,    -- ID - int
         15 ,    -- CraftCategoryID - int
         45 ,    -- BaseLevel - int
         N'Force Utility III' ,  -- ItemName - nvarchar(64)
         N'rune_sum3' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         22 ,    -- SkillID - int
         4 ,    -- CraftDeviceID - int
         96 ,    -- PerkID - int
         7 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         28 ,    -- MainComponentTypeID - int
         4 ,    -- MainMinimum - int
         0 ,    -- SecondaryComponentTypeID - int
         0 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         0 ,    -- EnhancementSlots - int
         5 ,    -- MainMaximum - int
         0 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         NULL      -- BaseStructureID - int
    )



-- Change Force Armor to CON/CHA/WIS
UPDATE dbo.Skill
SET [Primary] = 3,
	Secondary = 6,
	Tertiary = 5
WHERE ID = 11


-- Add Organic requirement to the description of Drain Life perk.
UPDATE dbo.Perk
SET Description = 'Deals damage to a single target and heals the user by a portion of damage dealt. Target must be organic.'
WHERE ID = 78



-- Remove the Dark Spread perk.
DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl
	WHERE PerkID = 14 
)

DELETE FROM dbo.PerkLevel
WHERE PerkID = 14

DELETE FROM dbo.Perk
WHERE ID = 14


-- Remove the Dark Spread custom effect
DELETE FROM dbo.PCCustomEffect
WHERE CustomEffectID = 10

DELETE FROM dbo.CustomEffect
WHERE ID = 10


-- Remove the Dark Heal perk
DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT pl.ID 
	FROM dbo.PerkLevel pl 
	WHERE pl.PerkID = 6
)

DELETE FROM dbo.PerkLevel
WHERE PerkID = 6

DELETE FROM dbo.Perk
WHERE ID = 6




-- Update Force Spread to affect multiple other perks.
UPDATE dbo.Perk
SET Description = 'Several force abilities will affect all party members within range while the effect is active.'
WHERE ID = 13

-- Level 1
UPDATE dbo.PerkLevel
SET Description = '1 use, 10m range, lasts 30 seconds. Perks Affected: Force Heal'
WHERE PerkID = 13 AND Level = 1

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 5
WHERE PerkLevelID = (
	SELECT TOP(1) ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = 13 AND pl.Level = 1
	ORDER BY pl.ID
)


-- Level 2
UPDATE dbo.PerkLevel
SET Description = '1 use, 15m range, lasts 1 minute. Perks Affected: Force Heal'
WHERE PerkID = 13 AND Level = 2

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 10
WHERE PerkLevelID = (
	SELECT TOP(1) ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = 13 AND pl.Level = 2
	ORDER BY pl.ID
)

-- Level 3
UPDATE dbo.PerkLevel
SET Description = '2 uses, 15m range, lasts 1 minute. Perks Affected: Force Heal, Force Aura'
WHERE PerkID = 13 AND Level = 3

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 15
WHERE PerkLevelID = (
	SELECT TOP(1) ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = 13 AND pl.Level = 3
	ORDER BY pl.ID
)


-- Level 4
UPDATE dbo.PerkLevel
SET Description = '2 uses, 20m range, lasts 2 minutes. Perks Affected: Force Heal, Force Aura'
WHERE PerkID = 13 AND Level = 4

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 20
WHERE PerkLevelID = (
	SELECT TOP(1) ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = 13 AND pl.Level = 4
	ORDER BY pl.ID
)



-- Level 5
UPDATE dbo.PerkLevel
SET Description = '3 uses, 20m range, lasts 2 minutes. Perks Affected: Force Heal, Force Aura'
WHERE PerkID = 13 AND Level = 5

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 25
WHERE PerkLevelID = (
	SELECT TOP(1) ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = 13 AND pl.Level = 5
	ORDER BY pl.ID
)


-- Level 6
UPDATE dbo.PerkLevel
SET Description = '4 uses, 20m range, lasts 2 minutes. Perks Affected: Force Heal, Force Aura'
WHERE PerkID = 13 AND Level = 6

UPDATE dbo.PerkLevelSkillRequirement
SET RequiredRank = 30
WHERE PerkLevelID = (
	SELECT TOP(1) ID 
	FROM dbo.PerkLevel pl
	WHERE pl.PerkID = 13 AND pl.Level = 6
	ORDER BY pl.ID
)




-- Remove Dark Heal from cooldown category
UPDATE dbo.CooldownCategory
SET Name = 'Force Heal'
WHERE ID = 6

-- Increase Force Aura FP price
UPDATE dbo.Perk
SET BaseFPCost = 8
WHERE ID = 76

-- Remove Dark Spread from cooldown category
UPDATE dbo.CooldownCategory
SET Name = 'Force Spread'
WHERE ID = 20