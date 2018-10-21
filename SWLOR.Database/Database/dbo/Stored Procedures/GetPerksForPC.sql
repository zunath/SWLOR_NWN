-- =============================================
-- Author:		zunath
-- Create date: 2018-08-11
-- Description:	Returns all perks available to purchase for PC
-- =============================================
CREATE PROCEDURE dbo.GetPerksForPC
	@PlayerID NVARCHAR(60),
	@CategoryID INT
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
           p.EnmityAdjustmentRuleID,
		   p.CastAnimationID
	FROM dbo.Perks p
	WHERE p.PerkID IN (
		SELECT DISTINCT p.PerkID
		FROM dbo.Perks p
		JOIN dbo.PlayerCharacters plc ON plc.PlayerID = @PlayerID
		LEFT JOIN dbo.PerkLevels pl ON pl.PerkID = p.PerkID
		LEFT JOIN dbo.PerkLevelSkillRequirements plsr ON plsr.PerkLevelID = pl.PerkLevelID
		LEFT JOIN dbo.PerkLevelQuestRequirements pqr ON pqr.PerkLevelID = pl.PerkLevelID
		LEFT JOIN dbo.PCSkills pcsk ON pcsk.PlayerID = plc.PlayerID AND plsr.SkillID = pcsk.SkillID
		LEFT JOIN dbo.PCQuestStatus pqs ON pqs.PlayerID = plc.PlayerID AND pqs.QuestID = pqr.RequiredQuestID
		WHERE (plsr.SkillID IS NULL OR plsr.SkillID = pcsk.SkillID)
			AND (plsr.RequiredRank IS NULL OR plsr.RequiredRank <= pcsk.Rank)
			AND (pqr.RequiredQuestID IS NULL OR (pqr.RequiredQuestID = pqs.QuestID AND pqs.CompletionDate IS NOT NULL))
			AND p.PerkCategoryID = @CategoryID
			AND p.IsActive = 1
	)

END


