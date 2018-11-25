
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

GO


SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
-- =============================================
-- Author:		zunath
-- Create date: 2018-11-18
-- Description:	Retrieve all associated player data for use in caching.
-- =============================================
ALTER PROCEDURE GetPlayerData
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
	 SELECT ID ,
            PlayerID ,
            SkillID ,
            XP ,
            Rank ,
            IsLocked 
	 FROM dbo.PCSkill 
	 WHERE PlayerID = @PlayerID
END

