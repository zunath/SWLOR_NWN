
-- Refund Force Breach
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 3 -- int

-- Refund Force Lightning
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 4 -- int

-- Refund Force Heal
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 5 -- int

-- Refund Absorption Field
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 11 -- int

-- Refund Force Spread
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 13 -- int

-- Refund Force Push
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 19 -- int

-- Refund Force Aura
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 76 -- int

-- Refund Drain Life
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 78 -- int

-- Refund Chainspell
EXEC dbo.ADM_RefundPlayerPerk @PerkID = 126 -- int


UPDATE dbo.Skill
SET Name = 'Force Alter',
	Description = 'Ability to use alter-based force abilities like Force Confusion and Force Push. Higher skill levels unlock new abilities.'
WHERE ID = 19 -- Force Combat

UPDATE dbo.Skill
SET Name = 'Force Control',
	Description = 'Ability to use control-based force abilities like Force Speed and Rage. Higher skill levels unlock new abilities.'
WHERE ID = 20 -- Force Support

UPDATE dbo.Skill
SET Name = 'Force Sense',
	Description = 'Ability to use sense-based force abilities like Force Stun and Premonition. Higher skill levels unlock new abilities.'
WHERE ID = 21 -- Force Utility


-- Sum up levels in Force Combat / Force Support / Force Utility and put them into the Skill Pool for later redistribution by the player.
INSERT INTO dbo.PCSkillPool ( ID ,
                              PlayerID ,
                              SkillCategoryID ,
                              Levels )
SELECT 
	NEWID(),
	pcs.PlayerID, 
	6, -- 6 = Force
	SUM(pcs.Rank)
FROM dbo.PCSkill pcs
WHERE pcs.SkillID IN (19, 20, 21)
GROUP BY pcs.PlayerID 
HAVING SUM(pcs.Rank) > 0

-- Set the skill levels to zero since they've been pooled.
UPDATE dbo.PCSkill
SET Rank = 0,
	XP = 0
WHERE SkillID IN (19, 20, 21)


-- Some of our perk names are carried over, but all of the functionality has changed.
-- Just to make my life a little easier, we're going to wipe all existing perk information
-- and start from scratch.

DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (
		3, 4, 5, 11, 13, 19, 76, 78, 126
	) 
)

DELETE FROM dbo.PCPerkRefund
WHERE PerkID IN (3, 4, 5, 11, 13, 19, 76, 78, 126)

DELETE FROM dbo.PerkFeat
WHERE PerkID IN (3, 4, 5, 11, 13, 19, 76, 78, 126)

DELETE FROM dbo.PerkLevelQuestRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel 
	WHERE PerkID IN (
		3, 4, 5, 11, 13, 19, 76, 78, 126
	)
)

DELETE FROM dbo.PerkLevel
WHERE PerkID IN(3, 4, 5, 11, 13, 19, 76, 78, 126)

DELETE FROM dbo.Perk
WHERE ID IN (3, 4, 5, 11, 13, 19, 76, 78, 126)

-- Set up the Specialization table
CREATE TABLE Specialization(
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL
)

INSERT INTO dbo.Specialization ( ID ,
                                 Name )
VALUES ( 0 , -- ID - int
         N'None' -- Name - nvarchar(32)
    )
INSERT INTO dbo.Specialization ( ID ,
                                 Name )
VALUES ( 1 , -- ID - int
         N'Guardian' -- Name - nvarchar(32)
    )
INSERT INTO dbo.Specialization ( ID ,
                                 Name )
VALUES ( 2 , -- ID - int
         N'Consular' -- Name - nvarchar(32)
    )
INSERT INTO dbo.Specialization ( ID ,
                                 Name )
VALUES ( 3 , -- ID - int
         N'Sentinel' -- Name - nvarchar(32)
    )

CREATE TABLE ForceBalanceType(
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL
)

INSERT INTO dbo.ForceBalanceType ( ID ,
                                   Name )
VALUES ( 0 , -- ID - int
         N'Universal' -- Name - nvarchar(32)
    )

INSERT INTO dbo.ForceBalanceType ( ID ,
                                   Name )
VALUES ( 1 , -- ID - int
         N'Light Side' -- Name - nvarchar(32)
    )

INSERT INTO dbo.ForceBalanceType ( ID ,
                                   Name )
VALUES ( 2 , -- ID - int
         N'Dark Side' -- Name - nvarchar(32)
    )

ALTER TABLE dbo.Perk
ADD SpecializationID INT NOT NULL DEFAULT 0
CONSTRAINT FK_Perk_SpecializationID FOREIGN KEY REFERENCES dbo.Specialization(ID)

ALTER TABLE dbo.Perk
ADD ForceBalanceTypeID INT NOT NULL DEFAULT 0
CONSTRAINT FK_Perk_ForceBalanceTypeID FOREIGN KEY REFERENCES dbo.ForceBalanceType(ID)

ALTER TABLE dbo.Player
ADD SpecializationID INT NOT NULL DEFAULT 0
CONSTRAINT FK_Player_SpecializationID FOREIGN KEY REFERENCES dbo.Specialization(ID)

-- Remove the old perk categories
DELETE FROM dbo.PerkCategory WHERE ID IN (29, 30, 31)

-- Move the sequences for other categories up.
UPDATE dbo.PerkCategory
SET Sequence = 28 WHERE ID = 36
UPDATE dbo.PerkCategory
SET Sequence = 29 WHERE ID = 37

-- Add the new perk categories.
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 40 ,    -- ID - int
         N'Force Alter - General' ,  -- Name - nvarchar(64)
         1, -- IsActive - bit
         30      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 41 ,    -- ID - int
         N'Force Alter - Guardian' ,  -- Name - nvarchar(64)
         1, -- IsActive - bit
         31      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 42 ,    -- ID - int
         N'Force Alter - Consular' ,  -- Name - nvarchar(64)
         1 , -- IsActive - bit
         32      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 43 ,    -- ID - int
         N'Force Control - General' ,  -- Name - nvarchar(64)
         1, -- IsActive - bit
         33      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 44 ,    -- ID - int
         N'Force Control - Guardian' ,  -- Name - nvarchar(64)
         1, -- IsActive - bit
         34      -- Sequence - int
    )
INSERT INTO dbo.PerkCategory ( ID ,
                               Name ,
                               IsActive ,
                               Sequence )
VALUES ( 45 ,    -- ID - int
         N'Force Control - Consular' ,  -- Name - nvarchar(64)
         1 , -- IsActive - bit
         35      -- Sequence - int
    )

-- Now we'll do the new perk inserts
