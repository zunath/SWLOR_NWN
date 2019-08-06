
ALTER TABLE dbo.PCQuestStatus
ADD QuestState INT NULL
GO

UPDATE dbo.PCQuestStatus
SET QuestState = qs.Sequence
FROM dbo.PCQuestStatus pcqs
JOIN dbo.QuestState qs ON qs.ID = pcqs.CurrentQuestStateID

ALTER TABLE dbo.PCQuestStatus
DROP CONSTRAINT FK_PCQuesttatus_CurrentQuesttateID



IF EXISTS (SELECT *  FROM sys.indexes  WHERE name='IX_PCQuestStatus_PlayerID' 
	AND object_id = OBJECT_ID('[dbo].[PCQuestStatus]'))
BEGIN
	DROP INDEX IX_PCQuestStatus_PlayerID ON dbo.PCQuestStatus;
END


EXEC dbo.ADM_Drop_Constraint @TableName = N'dbo.PCQuestStatus' , -- nvarchar(200)
                             @ColumnName = N'CurrentQuestStateID'  -- nvarchar(200)

EXEC dbo.ADM_Drop_Column @TableName = N'dbo.PCQuestStatus' , -- nvarchar(200)
                         @ColumnName = N'CurrentQuestStateID'  -- nvarchar(200)


GO 
ALTER TABLE dbo.PCQuestStatus
ALTER COLUMN QuestState INT NOT NULL
GO


-- Add missing loot table for kath hound meat quest.
INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 44 , -- ID - int
         N'Kath Hounds 2 - Viscara' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 44 ,    -- LootTableID - int
         'kath_meat_1' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

-- Update the GetPlayerData proc. Run this after all other DDL changes have run.

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
-- Updated 2019-08-03: Change PCQuestStatus column CurrentQuestStateID to QuestStateID
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
            QuestState ,
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
	 FROM dbo.PCGuildPoint
	 WHERE PlayerID = @PlayerID

	 
END


