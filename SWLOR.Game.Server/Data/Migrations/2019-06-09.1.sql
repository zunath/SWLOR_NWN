
CREATE TABLE Guild (
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(64) NOT NULL,
	Description NVARCHAR(1000) NOT NULL
)


INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 1 ,   -- ID - int
         N'Hunter''s Guild' , -- Name - nvarchar(64)
         N'Specializes in the detection and removal of threats across the galaxy.'   -- Description - nvarchar(64)
    )

INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 2 ,   -- ID - int
         N'Engineering Guild' , -- Name - nvarchar(64)
         N'Specializes in the construction of engineering and electronic items.'   -- Description - nvarchar(64)
    )

INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 3 ,   -- ID - int
         N'Weaponsmith Guild' , -- Name - nvarchar(64)
         N'Specializes in the construction of weaponry.'   -- Description - nvarchar(64)
    )

INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 4 ,   -- ID - int
         N'Armorsmith Guild' , -- Name - nvarchar(64)
         N'Specializes in the construction of armor.'   -- Description - nvarchar(64)
    )
	
INSERT INTO dbo.Guild ( ID ,
                        Name ,
                        Description )
VALUES ( 5 ,   -- ID - int
         N'Fabrication Guild' , -- Name - nvarchar(64)
         N'Specializes in the construction of buildings and furniture.'   -- Description - nvarchar(1000)
    )

	
CREATE TABLE PCGuildPoint(
	ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	PlayerID UNIQUEIDENTIFIER NOT NULL,
	GuildID INT NOT NULL,
	Rank INT NOT NULL,
	Points INT NOT NULL,

	CONSTRAINT FK_PCGuildPoint_PlayerID FOREIGN KEY(PlayerID)
		REFERENCES dbo.Player(ID),
	CONSTRAINT FK_PCGuildPoint_GuildID FOREIGN KEY(GuildID)
		REFERENCES dbo.Guild(ID),
	CONSTRAINT UQ_PCGuildPoint_PlayerIDGuildID UNIQUE(PlayerID, GuildID)
)





SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
-- =============================================
-- Author:		zunath
-- Create date: 2018-11-18
-- Description:	Retrieve all associated player data for use in caching.
-- Updated 2018-11-27: Retrieve BankItem records
-- Updated 2018-12-14: Retrieve PCSkillPool records
-- Updated 2019-03-11: Exclude impounded items which have been retrieved. Exclude impounded items
--					   which have not been retrieved after 30 days.
-- Updated 2019-03-31: Change the excluded impound items query to cast to DATE to help improve performance.
--					   According to various sources, DATEADD on a DATE is quicker than a DATETIME2
-- Updated 2019-06-09: Retrieve PCGuildPoint data.
-- =============================================
ALTER PROCEDURE [dbo].[GetPlayerData]
	@PlayerID UNIQUEIDENTIFIER
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
    -- Get PC cooldowns
	SELECT ID ,
           PlayerID ,
           CooldownCategoryID ,
           DateUnlocked 
	FROM dbo.PCCooldown
	WHERE PlayerID = @PlayerID
	
	
	-- Get PC crafted blueprints
	SELECT ID ,
           PlayerID ,
           CraftBlueprintID ,
           DateFirstCrafted 
	FROM dbo.PCCraftedBlueprint  
	WHERE PlayerID = @PlayerID
	-- Get PC Custom Effects
	SELECT ID ,
           PlayerID ,
           CustomEffectID ,
           Ticks ,
           EffectiveLevel ,
           Data ,
           CasterNWNObjectID ,
           StancePerkID 
	FROM dbo.PCCustomEffect 
	WHERE PlayerID = @PlayerID
	-- Get PC Impounded Items
	SELECT ID ,
           PlayerID ,
           ItemName ,
           ItemTag ,
           ItemResref ,
           ItemObject ,
           DateImpounded ,
           DateRetrieved 
	FROM dbo.PCImpoundedItem 
	WHERE PlayerID = @PlayerID
		AND DateRetrieved IS NULL 
		AND GETUTCDATE() < DATEADD(DAY, 30, CAST(DateImpounded AS DATE))
	-- Get PC Key Items
	SELECT ID ,
           PlayerID ,
           KeyItemID ,
           AcquiredDate 
	FROM dbo.PCKeyItem 
	WHERE PlayerID = @PlayerID
	-- Get PC Map Pin
	SELECT ID ,
           PlayerID ,
           AreaTag ,
           PositionX ,
           PositionY ,
           NoteText 
	FROM dbo.PCMapPin
	WHERE PlayerID = @PlayerID 
	-- Get PC Map Progression
	SELECT ID ,
           PlayerID ,
           AreaResref ,
           Progression 
	FROM dbo.PCMapProgression
	WHERE PlayerID = @PlayerID
	
	
	 -- Get PC Object Visibility
	 SELECT ID ,
            PlayerID ,
            VisibilityObjectID ,
            IsVisible 
	 FROM dbo.PCObjectVisibility 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Outfit
	 SELECT PlayerID ,
            Outfit1 ,
            Outfit2 ,
            Outfit3 ,
            Outfit4 ,
            Outfit5 ,
            Outfit6 ,
            Outfit7 ,
            Outfit8 ,
            Outfit9 ,
            Outfit10 
	 FROM dbo.PCOutfit
	 WHERE PlayerID = @PlayerID 
	 -- Get PC Overflow Items
	 SELECT ID ,
            PlayerID ,
            ItemName ,
            ItemTag ,
            ItemResref ,
            ItemObject 
	 FROM dbo.PCOverflowItem 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Perks
	 SELECT ID ,
            PlayerID ,
            AcquiredDate ,
            PerkID ,
            PerkLevel 
	 FROM dbo.PCPerk
	 WHERE PlayerID = @PlayerID 
	 -- Get PC Quest Item Progress
	 SELECT ID ,
            PlayerID ,
            PCQuestStatusID ,
            Resref ,
            Remaining ,
            MustBeCraftedByPlayer 
	 FROM dbo.PCQuestItemProgress 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Kill Target Progress
	 SELECT ID ,
            PlayerID ,
            PCQuestStatusID ,
            NPCGroupID ,
            RemainingToKill 
	 FROM dbo.PCQuestKillTargetProgress
	 WHERE PlayerID = @PlayerID 
	 -- Get PC Quest Status
	 SELECT ID ,
            PlayerID ,
            QuestID ,
            CurrentQuestStateID ,
            CompletionDate ,
            SelectedItemRewardID 
	 FROM dbo.PCQuestStatus 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Regional Fame
	 SELECT ID ,
            PlayerID ,
            FameRegionID ,
            Amount 
	 FROM dbo.PCRegionalFame 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Search Sites
	 SELECT ID ,
            PlayerID ,
            SearchSiteID ,
            UnlockDateTime 
	 FROM dbo.PCSearchSite 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Search Site Items
	 SELECT ID ,
            PlayerID ,
            SearchSiteID ,
            SearchItem
	 FROM dbo.PCSearchSiteItem 
	 WHERE PlayerID = @PlayerID
	 -- Get PC Skills
	 SELECT ID ,
            PlayerID ,
            SkillID ,
            XP ,
            Rank ,
            IsLocked 
	 FROM dbo.PCSkill 
	 WHERE PlayerID = @PlayerID
	 -- Get Bank Items
	 SELECT bi.ID ,
            bi.BankID ,
            bi.PlayerID ,
            bi.ItemID ,
            bi.ItemName ,
            bi.ItemTag ,
            bi.ItemResref ,
            bi.ItemObject ,
            bi.DateStored 
	 FROM dbo.BankItem bi
	 WHERE bi.PlayerID = @PlayerID 
	 -- Get PC Skill Pools
	 SELECT pcsp.ID ,
            pcsp.PlayerID ,
            pcsp.SkillCategoryID ,
            pcsp.Levels 
	 FROM dbo.PCSkillPool pcsp
	 WHERE pcsp.PlayerID = @PlayerID 
	 -- Get PC Guild Point data
	 SELECT ID ,
            PlayerID ,
            GuildID ,
            Rank ,
            Points 
	 FROM PCGuildPoint
	 WHERE PlayerID = @PlayerID
END

ALTER TABLE dbo.PCQuestStatus
ADD TimesCompleted INT NOT NULL DEFAULT 0

UPDATE dbo.PCQuestStatus
SET TimesCompleted = 1
WHERE CompletionDate IS NOT NULL


ALTER TABLE dbo.Quest
ADD RewardGuildID INT NULL
CONSTRAINT FK_Quest_RewardGuildID FOREIGN KEY REFERENCES dbo.Guild(ID)

ALTER TABLE dbo.Quest
ADD RewardGuildPoints INT NOT NULL DEFAULT 0


ALTER TABLE dbo.ServerConfiguration
ADD LastGuildTaskUpdate DATETIME2 NOT NULL DEFAULT '1900-01-01'

CREATE TABLE GuildTask(
	ID INT PRIMARY KEY NOT NULL IDENTITY,
	GuildID INT NOT NULL,
	QuestID INT NOT NULL,
	RequiredRank INT NOT NULL,
	IsCurrentlyOffered BIT NOT NULL,

	CONSTRAINT FK_GuildTask_GuildID FOREIGN KEY(GuildID)
		REFERENCES Guild(ID),
	CONSTRAINT FK_GuildTask_QuestID FOREIGN KEY(QuestID)
		REFERENCES dbo.Quest(ID)
)



/*
The following query was made to generate the following insert statements. 


SELECT 
	-- Begin quest insert
	'INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (' 
	+ CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000)) + ', '  -- ID
	+ '''Armorsmith Guild Task: 1x ' + ItemName + ''', ' -- Name
	+ '''arm_tsk_' + CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000))  + ''', ' -- JournalTag
	+ '1, ' -- FameRegionID
	+ '0, ' -- RequiredFameAmount
	+ '0, ' -- AllowRewardSelection
	+ CAST(50 * RequiredPerkLevel + (MainMinimum * 20 + SecondaryMinimum * 15 + TertiaryMinimum * 10) AS NVARCHAR(1000)) + ', ' -- RewardGold
	+ 'NULL, ' -- RewardKeyItem
	+ '0, ' -- RewardFame
	+ '1, ' -- IsRepeatable
	+ ''''',' -- MapNoteTag
	+ 'NULL, ' -- StartKeyItemID
	+ '0, ' -- RemoveStartKeyItemAfterCompletion
	+ ''''',' -- OnAcceptRule
	+ ''''',' -- OnAdvanceRule
	+ ''''',' -- OnCompleteRule
	+ ''''',' -- OnKillTargetRule
	+ ''''',' -- OnAcceptArgs
	+ ''''',' -- OnAdvanceArgs
	+ ''''',' -- OnCompleteArgs
	+ ''''',' -- OnKillTargetArgs
	+ '1, ' -- RewardGuildID
	+ CAST(10 * RequiredPerkLevel + (MainMinimum * 5 + SecondaryMinimum * 4 + TertiaryMinimum * 3) AS NVARCHAR(1000)) -- RewardGuildPoints
	+ ');', 
	-- End Quest Insert

	-- Begin quest state + required item insert
	'INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES ('
	+ CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000)) + ', '  -- QuestID
	+ '1, ' -- Sequence
	+ '4, ' -- QuestTypeID
	+ '1' -- JournalStateID
	+ '); '
	+ 'INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES ('
	+ CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000)) + ', '  -- QuestID
	+ '''' + ItemResref + ''', ' -- Resref
	+ '1, ' -- Quantity
	+ 'SCOPE_IDENTITY(), ' -- QuestStateID (SCOPE_IDENTITY() of the previous QuestState insert
	+ '1' -- MustBeCraftedByPlayer
	+ ');'

	-- End quest state + required item insert
	, 'INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES ('
	+ '1, ' -- GuildID
	+ CAST(100 + (ROW_NUMBER() OVER (ORDER BY RequiredPerkLevel, ItemName)) AS NVARCHAR(1000)) + ', ' -- QuestID
	+ CASE RequiredPerkLevel
		WHEN 0 THEN '1'
		WHEN 1 THEN '2'
		WHEN 3 THEN '3'
		WHEN 5 THEN '4'
		WHEN 7 THEN '5'
		ELSE '0'
	  END + ', '  -- RequiredRank
	+ '0' -- IsCurrentlyOffered
	+ ');'
FROM dbo.CraftBlueprint 
WHERE CraftDeviceID = 1
ORDER BY RequiredPerkLevel ASC, ItemName



*/


INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (101, 'Armorsmith Guild Task: 1x Basic Breastplate', 'arm_tsk_101', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (102, 'Armorsmith Guild Task: 1x Basic Force Boots', 'arm_tsk_102', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (103, 'Armorsmith Guild Task: 1x Basic Force Helmet', 'arm_tsk_103', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (104, 'Armorsmith Guild Task: 1x Basic Force Robes', 'arm_tsk_104', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (105, 'Armorsmith Guild Task: 1x Basic Heavy Boots', 'arm_tsk_105', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (106, 'Armorsmith Guild Task: 1x Basic Heavy Helmet', 'arm_tsk_106', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (107, 'Armorsmith Guild Task: 1x Basic Large Shield', 'arm_tsk_107', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (108, 'Armorsmith Guild Task: 1x Basic Leather Tunic', 'arm_tsk_108', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (109, 'Armorsmith Guild Task: 1x Basic Light Boots', 'arm_tsk_109', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (110, 'Armorsmith Guild Task: 1x Basic Light Helmet', 'arm_tsk_110', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (111, 'Armorsmith Guild Task: 1x Basic Power Glove', 'arm_tsk_111', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (112, 'Armorsmith Guild Task: 1x Basic Small Shield', 'arm_tsk_112', 1, 0, 0, 35, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 9);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (113, 'Armorsmith Guild Task: 1x Basic Tower Shield', 'arm_tsk_113', 1, 0, 0, 70, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 18);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (114, 'Armorsmith Guild Task: 1x Fiberplast Padding', 'arm_tsk_114', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (115, 'Armorsmith Guild Task: 1x Force Armor Core', 'arm_tsk_115', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (116, 'Armorsmith Guild Task: 1x Force Armor Segment', 'arm_tsk_116', 1, 0, 0, 60, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 15);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (117, 'Armorsmith Guild Task: 1x Heavy Armor Core', 'arm_tsk_117', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (118, 'Armorsmith Guild Task: 1x Heavy Armor Segment', 'arm_tsk_118', 1, 0, 0, 80, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 20);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (119, 'Armorsmith Guild Task: 1x Light Armor Core', 'arm_tsk_119', 1, 0, 0, 20, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 5);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (120, 'Armorsmith Guild Task: 1x Light Armor Segment', 'arm_tsk_120', 1, 0, 0, 40, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 10);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (121, 'Armorsmith Guild Task: 1x Metal Reinforcement', 'arm_tsk_121', 1, 0, 0, 55, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 14);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (122, 'Armorsmith Guild Task: 1x Breastplate I', 'arm_tsk_122', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (123, 'Armorsmith Guild Task: 1x Force Armor Repair Kit I', 'arm_tsk_123', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (124, 'Armorsmith Guild Task: 1x Force Belt I', 'arm_tsk_124', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (125, 'Armorsmith Guild Task: 1x Force Boots I', 'arm_tsk_125', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (126, 'Armorsmith Guild Task: 1x Force Helmet I', 'arm_tsk_126', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (127, 'Armorsmith Guild Task: 1x Force Necklace I', 'arm_tsk_127', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (128, 'Armorsmith Guild Task: 1x Force Robes I', 'arm_tsk_128', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (129, 'Armorsmith Guild Task: 1x Heavy Armor Repair Kit I', 'arm_tsk_129', 1, 0, 0, 155, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 37);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (130, 'Armorsmith Guild Task: 1x Heavy Belt I', 'arm_tsk_130', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (131, 'Armorsmith Guild Task: 1x Heavy Boots I', 'arm_tsk_131', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (132, 'Armorsmith Guild Task: 1x Heavy Crest I', 'arm_tsk_132', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (133, 'Armorsmith Guild Task: 1x Heavy Helmet I', 'arm_tsk_133', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (134, 'Armorsmith Guild Task: 1x Large Shield I', 'arm_tsk_134', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (135, 'Armorsmith Guild Task: 1x Leather Tunic I', 'arm_tsk_135', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (136, 'Armorsmith Guild Task: 1x Light Armor Repair Kit I', 'arm_tsk_136', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (137, 'Armorsmith Guild Task: 1x Light Belt I', 'arm_tsk_137', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (138, 'Armorsmith Guild Task: 1x Light Boots I', 'arm_tsk_138', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (139, 'Armorsmith Guild Task: 1x Light Choker I', 'arm_tsk_139', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (140, 'Armorsmith Guild Task: 1x Light Helmet I', 'arm_tsk_140', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (141, 'Armorsmith Guild Task: 1x Power Glove I', 'arm_tsk_141', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (142, 'Armorsmith Guild Task: 1x Shield Repair Kit I', 'arm_tsk_142', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (143, 'Armorsmith Guild Task: 1x Small Shield I', 'arm_tsk_143', 1, 0, 0, 85, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 19);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (144, 'Armorsmith Guild Task: 1x Tower Shield I', 'arm_tsk_144', 1, 0, 0, 120, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 28);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (145, 'Armorsmith Guild Task: 1x Breastplate II', 'arm_tsk_145', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (146, 'Armorsmith Guild Task: 1x Force Armor Repair Kit II', 'arm_tsk_146', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (147, 'Armorsmith Guild Task: 1x Force Belt II', 'arm_tsk_147', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (148, 'Armorsmith Guild Task: 1x Force Boots II', 'arm_tsk_148', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (149, 'Armorsmith Guild Task: 1x Force Helmet II', 'arm_tsk_149', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (150, 'Armorsmith Guild Task: 1x Force Necklace II', 'arm_tsk_150', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (151, 'Armorsmith Guild Task: 1x Force Robes II', 'arm_tsk_151', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (152, 'Armorsmith Guild Task: 1x Heavy Armor Repair Kit II', 'arm_tsk_152', 1, 0, 0, 255, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 57);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (153, 'Armorsmith Guild Task: 1x Heavy Belt II', 'arm_tsk_153', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (154, 'Armorsmith Guild Task: 1x Heavy Boots II', 'arm_tsk_154', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (155, 'Armorsmith Guild Task: 1x Heavy Crest II', 'arm_tsk_155', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (156, 'Armorsmith Guild Task: 1x Heavy Helmet II', 'arm_tsk_156', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (157, 'Armorsmith Guild Task: 1x Heavy Helmet III', 'arm_tsk_157', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (158, 'Armorsmith Guild Task: 1x Large Shield II', 'arm_tsk_158', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (159, 'Armorsmith Guild Task: 1x Leather Tunic II', 'arm_tsk_159', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (160, 'Armorsmith Guild Task: 1x Light Armor Repair Kit II', 'arm_tsk_160', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (161, 'Armorsmith Guild Task: 1x Light Belt II', 'arm_tsk_161', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (162, 'Armorsmith Guild Task: 1x Light Boots II', 'arm_tsk_162', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (163, 'Armorsmith Guild Task: 1x Light Choker II', 'arm_tsk_163', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (164, 'Armorsmith Guild Task: 1x Light Helmet II', 'arm_tsk_164', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (165, 'Armorsmith Guild Task: 1x Power Glove II', 'arm_tsk_165', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (166, 'Armorsmith Guild Task: 1x Shield Repair Kit II', 'arm_tsk_166', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (167, 'Armorsmith Guild Task: 1x Small Shield II', 'arm_tsk_167', 1, 0, 0, 185, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 39);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (168, 'Armorsmith Guild Task: 1x Tower Shield II', 'arm_tsk_168', 1, 0, 0, 220, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 48);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (169, 'Armorsmith Guild Task: 1x Additional Fuel Tank (Small)', 'arm_tsk_169', 1, 0, 0, 305, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 64);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (170, 'Armorsmith Guild Task: 1x Additional Stronidium Tank (Small)', 'arm_tsk_170', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (171, 'Armorsmith Guild Task: 1x Breastplate III', 'arm_tsk_171', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (172, 'Armorsmith Guild Task: 1x Force Armor Repair Kit III', 'arm_tsk_172', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (173, 'Armorsmith Guild Task: 1x Force Belt III', 'arm_tsk_173', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (174, 'Armorsmith Guild Task: 1x Force Boots III', 'arm_tsk_174', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (175, 'Armorsmith Guild Task: 1x Force Helmet III', 'arm_tsk_175', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (176, 'Armorsmith Guild Task: 1x Force Necklace III', 'arm_tsk_176', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (177, 'Armorsmith Guild Task: 1x Force Robes III', 'arm_tsk_177', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (178, 'Armorsmith Guild Task: 1x Heavy Armor Repair Kit III', 'arm_tsk_178', 1, 0, 0, 355, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 77);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (179, 'Armorsmith Guild Task: 1x Heavy Belt III', 'arm_tsk_179', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (180, 'Armorsmith Guild Task: 1x Heavy Boots III', 'arm_tsk_180', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (181, 'Armorsmith Guild Task: 1x Heavy Crest III', 'arm_tsk_181', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (182, 'Armorsmith Guild Task: 1x Large Shield III', 'arm_tsk_182', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (183, 'Armorsmith Guild Task: 1x Leather Tunic III', 'arm_tsk_183', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (184, 'Armorsmith Guild Task: 1x Light Armor Repair Kit III', 'arm_tsk_184', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (185, 'Armorsmith Guild Task: 1x Light Belt III', 'arm_tsk_185', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (186, 'Armorsmith Guild Task: 1x Light Boots III', 'arm_tsk_186', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (187, 'Armorsmith Guild Task: 1x Light Choker III', 'arm_tsk_187', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (188, 'Armorsmith Guild Task: 1x Light Helmet III', 'arm_tsk_188', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (189, 'Armorsmith Guild Task: 1x Power Glove III', 'arm_tsk_189', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (190, 'Armorsmith Guild Task: 1x Shield Repair Kit III', 'arm_tsk_190', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (191, 'Armorsmith Guild Task: 1x Small Shield III', 'arm_tsk_191', 1, 0, 0, 285, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 59);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (192, 'Armorsmith Guild Task: 1x Tower Shield III', 'arm_tsk_192', 1, 0, 0, 320, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 68);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (193, 'Armorsmith Guild Task: 1x Additional Fuel Tank (Medium)', 'arm_tsk_193', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 84);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (194, 'Armorsmith Guild Task: 1x Additional Stronidium Tank (Medium)', 'arm_tsk_194', 1, 0, 0, 405, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 84);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (195, 'Armorsmith Guild Task: 1x Breastplate IV', 'arm_tsk_195', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (196, 'Armorsmith Guild Task: 1x Force Armor Repair Kit IV', 'arm_tsk_196', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (197, 'Armorsmith Guild Task: 1x Force Boots IV', 'arm_tsk_197', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (198, 'Armorsmith Guild Task: 1x Force Helmet IV', 'arm_tsk_198', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (199, 'Armorsmith Guild Task: 1x Force Necklace IV', 'arm_tsk_199', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (200, 'Armorsmith Guild Task: 1x Force Robes IV', 'arm_tsk_200', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (201, 'Armorsmith Guild Task: 1x Heavy Armor Repair Kit IV', 'arm_tsk_201', 1, 0, 0, 455, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 97);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (202, 'Armorsmith Guild Task: 1x Heavy Boots IV', 'arm_tsk_202', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (203, 'Armorsmith Guild Task: 1x Heavy Crest IV', 'arm_tsk_203', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (204, 'Armorsmith Guild Task: 1x Heavy Helmet IV', 'arm_tsk_204', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (205, 'Armorsmith Guild Task: 1x Hull Plating', 'arm_tsk_205', 1, 0, 0, 430, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 90);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (206, 'Armorsmith Guild Task: 1x Large Shield IV', 'arm_tsk_206', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (207, 'Armorsmith Guild Task: 1x Leather Tunic IV', 'arm_tsk_207', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (208, 'Armorsmith Guild Task: 1x Light Armor Repair Kit IV', 'arm_tsk_208', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (209, 'Armorsmith Guild Task: 1x Light Boots IV', 'arm_tsk_209', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (210, 'Armorsmith Guild Task: 1x Light Choker IV', 'arm_tsk_210', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (211, 'Armorsmith Guild Task: 1x Light Helmet IV', 'arm_tsk_211', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (212, 'Armorsmith Guild Task: 1x Power Glove IV', 'arm_tsk_212', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (213, 'Armorsmith Guild Task: 1x Prism Force Necklace', 'arm_tsk_213', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (214, 'Armorsmith Guild Task: 1x Prism Heavy Necklace', 'arm_tsk_214', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (215, 'Armorsmith Guild Task: 1x Prism Light Necklace', 'arm_tsk_215', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (216, 'Armorsmith Guild Task: 1x Prismatic Force Belt', 'arm_tsk_216', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (217, 'Armorsmith Guild Task: 1x Prismatic Heavy Belt', 'arm_tsk_217', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (218, 'Armorsmith Guild Task: 1x Prismatic Light Belt', 'arm_tsk_218', 1, 0, 0, 395, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 82);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (219, 'Armorsmith Guild Task: 1x Shield Repair Kit IV', 'arm_tsk_219', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 88);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (220, 'Armorsmith Guild Task: 1x Small Shield IV', 'arm_tsk_220', 1, 0, 0, 385, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 79);
INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs, RewardGuildID, RewardGuildPoints ) VALUES (221, 'Armorsmith Guild Task: 1x Tower Shield IV', 'arm_tsk_221', 1, 0, 0, 420, NULL, 0, 1, '',NULL, 0, '','','','','','','','',1, 88);


INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (101, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (101, 'breastplate_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (102, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (102, 'force_boots_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (103, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (103, 'helmet_fb', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (104, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (104, 'force_robe_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (105, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (105, 'heavy_boots_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (106, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (106, 'helmet_hb', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (107, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (107, 'large_shield_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (108, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (108, 'leather_tunic_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (109, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (109, 'light_boots_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (110, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (110, 'helmet_lb', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (111, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (111, 'powerglove_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (112, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (112, 'small_shield_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (113, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (113, 'tower_shield_b', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (114, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (114, 'padding_fiber', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (115, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (115, 'core_f_armor', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (116, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (116, 'f_armor_segment', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (117, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (117, 'core_h_armor', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (118, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (118, 'h_armor_segment', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (119, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (119, 'core_l_armor', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (120, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (120, 'l_armor_segment', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (121, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (121, 'padding_metal', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (122, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (122, 'breastplate_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (123, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (123, 'fa_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (124, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (124, 'force_belt_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (125, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (125, 'force_boots_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (126, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (126, 'helmet_f1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (127, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (127, 'force_neck_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (128, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (128, 'force_robe_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (129, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (129, 'ha_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (130, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (130, 'heavy_belt_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (131, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (131, 'heavy_boots_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (132, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (132, 'h_crest_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (133, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (133, 'helmet_h1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (134, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (134, 'large_shield_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (135, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (135, 'leather_tunic_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (136, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (136, 'la_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (137, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (137, 'light_belt_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (138, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (138, 'light_boots_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (139, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (139, 'lt_choker_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (140, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (140, 'helmet_l1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (141, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (141, 'powerglove_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (142, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (142, 'sh_rep_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (143, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (143, 'small_shield_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (144, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (144, 'tower_shield_1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (145, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (145, 'breastplate_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (146, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (146, 'fa_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (147, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (147, 'force_belt_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (148, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (148, 'force_boots_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (149, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (149, 'helmet_f2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (150, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (150, 'force_neck_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (151, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (151, 'force_robe_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (152, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (152, 'ha_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (153, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (153, 'heavy_belt_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (154, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (154, 'heavy_boots_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (155, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (155, 'h_crest_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (156, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (156, 'helmet_h2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (157, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (157, 'helmet_h3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (158, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (158, 'large_shield_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (159, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (159, 'leather_tunic_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (160, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (160, 'la_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (161, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (161, 'light_belt_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (162, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (162, 'light_boots_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (163, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (163, 'lt_choker_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (164, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (164, 'helmet_l2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (165, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (165, 'powerglove_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (166, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (166, 'sh_rep_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (167, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (167, 'small_shield_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (168, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (168, 'tower_shield_2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (169, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (169, 'ssfuel1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (170, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (170, 'ssstron1', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (171, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (171, 'breastplate_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (172, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (172, 'fa_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (173, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (173, 'force_belt_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (174, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (174, 'force_boots_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (175, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (175, 'helmet_f3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (176, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (176, 'force_neck_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (177, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (177, 'force_robe_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (178, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (178, 'ha_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (179, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (179, 'heavy_belt_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (180, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (180, 'heavy_boots_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (181, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (181, 'h_crest_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (182, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (182, 'large_shield_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (183, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (183, 'leather_tunic_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (184, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (184, 'la_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (185, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (185, 'light_belt_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (186, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (186, 'light_boots_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (187, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (187, 'lt_choker_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (188, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (188, 'helmet_l3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (189, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (189, 'powerglove_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (190, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (190, 'sh_rep_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (191, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (191, 'small_shield_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (192, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (192, 'tower_shield_3', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (193, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (193, 'ssfuel2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (194, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (194, 'ssstron2', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (195, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (195, 'breastplate_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (196, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (196, 'fa_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (197, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (197, 'force_boots_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (198, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (198, 'helmet_f4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (199, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (199, 'force_neck_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (200, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (200, 'force_robe_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (201, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (201, 'ha_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (202, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (202, 'heavy_boots_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (203, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (203, 'h_crest_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (204, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (204, 'helmet_h4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (205, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (205, 'hull_plating', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (206, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (206, 'large_shield_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (207, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (207, 'leather_tunic_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (208, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (208, 'la_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (209, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (209, 'light_boots_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (210, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (210, 'lt_choker_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (211, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (211, 'helmet_l4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (212, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (212, 'powerglove_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (213, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (213, 'prism_neck_f', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (214, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (214, 'prism_neck_h', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (215, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (215, 'prism_neck_l', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (216, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (216, 'prism_belt_f', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (217, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (217, 'prism_belt_h', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (218, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (218, 'prism_belt_l', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (219, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (219, 'sh_rep_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (220, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (220, 'small_shield_4', 1, SCOPE_IDENTITY(), 1);
INSERT INTO dbo.QuestState ( QuestID ,Sequence ,QuestTypeID ,JournalStateID ) VALUES (221, 1, 4, 1); INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )VALUES (221, 'tower_shield_4', 1, SCOPE_IDENTITY(), 1);

INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 101, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 102, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 103, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 104, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 105, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 106, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 107, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 108, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 109, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 110, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 111, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 112, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 113, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 114, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 115, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 116, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 117, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 118, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 119, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 120, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 121, 1, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 122, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 123, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 124, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 125, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 126, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 127, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 128, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 129, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 130, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 131, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 132, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 133, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 134, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 135, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 136, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 137, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 138, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 139, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 140, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 141, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 142, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 143, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 144, 2, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 145, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 146, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 147, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 148, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 149, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 150, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 151, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 152, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 153, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 154, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 155, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 156, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 157, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 158, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 159, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 160, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 161, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 162, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 163, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 164, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 165, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 166, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 167, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 168, 3, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 169, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 170, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 171, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 172, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 173, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 174, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 175, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 176, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 177, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 178, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 179, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 180, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 181, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 182, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 183, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 184, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 185, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 186, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 187, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 188, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 189, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 190, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 191, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 192, 4, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 193, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 194, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 195, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 196, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 197, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 198, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 199, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 200, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 201, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 202, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 203, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 204, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 205, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 206, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 207, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 208, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 209, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 210, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 211, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 212, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 213, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 214, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 215, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 216, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 217, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 218, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 219, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 220, 5, 0);
INSERT INTO dbo.GuildTask ( GuildID ,QuestID ,RequiredRank ,IsCurrentlyOffered )VALUES (1, 221, 5, 0);

