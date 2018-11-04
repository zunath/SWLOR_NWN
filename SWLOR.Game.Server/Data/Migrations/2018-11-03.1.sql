-- =============================================
-- Author:		zunath
-- Create date: 2018-11-03
-- Description:	Returns all loot tables and associated loot table items.
--				If a @Key is passed in, only one record matching that key will be returned.
-- =============================================
CREATE PROCEDURE GetLootTables
	@Key INT = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT lt.LootTableID ,
           lt.Name ,
           lti.LootTableItemID ,
           lti.Resref ,
           lti.MaxQuantity ,
           lti.Weight ,
           lti.IsActive ,
           lti.SpawnRule 
	FROM dbo.LootTables lt
	JOIN dbo.LootTableItems lti ON lti.LootTableID = lt.LootTableID 
	WHERE COALESCE(@Key, lt.LootTableID) = lt.LootTableID

END
GO

-- =============================================
-- Author:		zunath
-- Create date: 2018-11-03
-- Description:	Returns all perks and associated children
--				If a @Key is passed in, only one record matching that key will be returned.
-- =============================================
CREATE PROCEDURE GetPerks
	@Key INT = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT p.PerkID ,
           p.Name ,
           p.FeatID ,
           p.IsActive ,
           p.ScriptName ,
           p.BaseFPCost ,
           p.BaseCastingTime ,
           p.Description ,
           p.PerkCategoryID ,
           p.CooldownCategoryID ,
           p.ExecutionTypeID ,
           p.ItemResref ,
           p.IsTargetSelfOnly ,
           p.Enmity ,
           p.EnmityAdjustmentRuleID ,
           p.CastAnimationID ,
           pl.PerkLevelID ,
           pl.Level ,
           pl.Price ,
           pl.Description ,
           pls.PerkLevelSkillRequirementID ,
           pls.SkillID ,
           pls.RequiredRank ,
           plq.PerkLevelQuestRequirementID ,
           plq.RequiredQuestID 
	FROM dbo.Perks p
	JOIN dbo.PerkLevels pl ON pl.PerkID = p.PerkID
	LEFT JOIN dbo.PerkLevelSkillRequirements pls ON pls.PerkLevelID = pl.PerkLevelID
	LEFT JOIN dbo.PerkLevelQuestRequirements plq ON plq.PerkLevelID = pl.PerkLevelID
	WHERE COALESCE(@Key, p.PerkID) = p.PerkID

END

GO

-- =============================================
-- Author:		zunath
-- Create date: 2018-11-03
-- Description:	Returns all quests and associated children
--				If a @Key is passed in, only one record matching that key will be returned.
-- =============================================
CREATE PROCEDURE GetQuests
	@Key INT = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT q.QuestID ,
           q.Name ,
           q.JournalTag ,
           q.FameRegionID ,
           q.RequiredFameAmount ,
           q.AllowRewardSelection ,
           q.RewardGold ,
           q.RewardKeyItemID ,
           q.RewardFame ,
           q.IsRepeatable ,
           q.MapNoteTag ,
           q.StartKeyItemID ,
           q.RemoveStartKeyItemAfterCompletion ,
           q.OnAcceptRule ,
           q.OnAdvanceRule ,
           q.OnCompleteRule ,
           q.OnKillTargetRule ,
           q.OnAcceptArgs ,
           q.OnAdvanceArgs ,
           q.OnCompleteArgs ,
           q.OnKillTargetArgs ,
           qs.QuestStateID ,
           qs.Sequence ,
           qs.QuestTypeID ,
           qs.JournalStateID ,
           kt.QuestKillTargetListID ,
           kt.NPCGroupID ,
           kt.Quantity ,
           ri.QuestRequiredItemListID ,
           ri.Resref ,
           ri.Quantity ,
           ri.MustBeCraftedByPlayer ,
           qr.QuestPrerequisiteID ,
           qr.RequiredQuestID ,
           rki.QuestRequiredKeyItemID ,
           rki.KeyItemID ,
           qri.QuestRewardItemID ,
           qri.Resref ,
           qri.Quantity 
	FROM dbo.Quests q
	JOIN dbo.QuestStates qs ON qs.QuestID = q.QuestID
	LEFT JOIN dbo.QuestKillTargetList kt ON kt.QuestStateID = qs.QuestStateID
	LEFT JOIN dbo.QuestRequiredItemList ri ON ri.QuestStateID = qs.QuestStateID
	LEFT JOIN dbo.QuestPrerequisites qr ON qr.QuestID = q.QuestID
	LEFT JOIN dbo.QuestRequiredKeyItemList rki ON rki.QuestStateID = qs.QuestStateID
	LEFT JOIN dbo.QuestRewardItems qri ON qri.QuestID = q.QuestID 
	WHERE COALESCE(@Key, q.QuestID) = q.QuestID

END
GO

-- =============================================
-- Author:		zunath
-- Create date: 2018-11-03
-- Description:	Returns all skills and associated children
--				If a @Key is passed in, only one record matching that key will be returned.
-- =============================================
CREATE PROCEDURE GetSkills
	@Key INT = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT s.SkillID ,
           s.SkillCategoryID ,
           s.Name ,
           s.MaxRank ,
           s.IsActive ,
           s.Description ,
           s.[Primary] ,
           s.Secondary ,
           s.Tertiary ,
           s.ContributesToSkillCap ,
           sr.SkillXPRequirementID ,
           sr.Rank ,
           sr.XP 
	FROM dbo.Skills s
	JOIN dbo.SkillXPRequirement sr ON sr.SkillID = s.SkillID 
	WHERE COALESCE(@Key, s.SkillID) = s.SkillID

END
GO


-- =============================================
-- Author:		zunath
-- Create date: 2018-11-03
-- Description:	Returns all skills and associated children
--				If a @Key is passed in, only one record matching that key will be returned.
-- =============================================
CREATE PROCEDURE GetSpawns
	@Key INT = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	 SELECT s.SpawnID ,
           s.Name ,
           s.SpawnObjectTypeID ,
           so.SpawnObjectID ,
           so.Resref ,
           so.Weight ,
           so.SpawnRule ,
           so.NPCGroupID ,
           so.BehaviourScript ,
           so.DeathVFXID 
	 FROM dbo.Spawns s
	 JOIN dbo.SpawnObjects so ON so.SpawnID = s.SpawnID 
	 WHERE COALESCE(@Key, s.SpawnID) = s.SpawnID

END
GO


/****** Object:  Table [dbo].[Backgrounds]    Script Date: 11/3/2018 5:19:02 PM ******/
DROP TABLE [dbo].[Backgrounds]
GO


-- =============================================
-- Author:		zunath
-- Create date: 2018-11-03
-- Description:	Returns all banks and associated children
--				If a @Key is passed in, only one record matching that key will be returned.
-- =============================================
CREATE PROCEDURE GetBanks
	@Key INT  = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	 SELECT b.BankID ,
           b.AreaName ,
           b.AreaTag ,
           b.AreaResref ,
           bi.BankItemID ,
           bi.BankID ,
           bi.PlayerID ,
           bi.ItemID ,
           bi.ItemName ,
           bi.ItemTag ,
           bi.ItemResref ,
           bi.ItemObject ,
           bi.DateStored 
	 FROM dbo.Banks b
	 LEFT JOIN dbo.BankItems bi ON bi.BankID = b.BankID 
	 WHERE COALESCE(@Key, b.BankID) = b.BankID

END
GO

-- =============================================
-- Author:		zunath
-- Create date: 2018-11-03
-- Description:	Returns all areas and associated children
--				If a @Key is passed in, only one record matching that key will be returned.
-- =============================================
CREATE PROCEDURE GetAreas
	@Key NVARCHAR(60) = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT a.AreaID ,
           a.Resref ,
           a.Name ,
           a.Tag ,
           a.ResourceSpawnTableID ,
           a.Width ,
           a.Height ,
           a.IsBuildable ,
           a.NorthwestOwner ,
           a.NortheastOwner ,
           a.SouthwestOwner ,
           a.SoutheastOwner ,
           a.IsActive ,
           a.PurchasePrice ,
           a.DailyUpkeep ,
           a.DateLastBaked ,
           a.AutoSpawnResources ,
           a.ResourceQuality ,
           a.NorthwestLootTableID ,
           a.NortheastLootTableID ,
           a.SouthwestLootTableID ,
           a.SoutheastLootTableID ,
           a.MaxResourceQuality ,
           aw.AreaWalkmeshID ,
           aw.LocationX ,
           aw.LocationY ,
           aw.LocationZ 
	 FROM dbo.Areas a
	 JOIN dbo.AreaWalkmesh aw ON aw.AreaID = a.AreaID
	 WHERE COALESCE(@Key, a.AreaID) = a.AreaID
END



