-- =============================================
-- Author:		zunath
-- Create date: 2018-08-11
-- Description:	Get available perk categories for PC
-- =============================================
CREATE PROCEDURE dbo.GetPerkCategoriesForPC
	@PlayerID NVARCHAR(60)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT pc.PerkCategoryID ,
		   pc.Name ,
		   pc.IsActive ,
		   pc.Sequence
	FROM dbo.PerkCategories pc
	WHERE pc.PerkCategoryID IN (
		SELECT DISTINCT pc.PerkCategoryID
		FROM dbo.PerkCategories pc
		JOIN dbo.Perks p ON p.PerkCategoryID = pc.PerkCategoryID
		JOIN dbo.PlayerCharacters plc ON plc.PlayerID = @PlayerID
		LEFT JOIN dbo.PerkLevels pl ON pl.PerkID = p.PerkID
		LEFT JOIN dbo.PerkLevelSkillRequirements psr ON psr.PerkLevelID = pl.PerkLevelID
		LEFT JOIN dbo.PerkLevelQuestRequirements pqr ON pqr.PerkLevelID = pl.PerkLevelID
		LEFT JOIN dbo.PCSkills pcsk ON pcsk.PlayerID = plc.PlayerID AND pcsk.SkillID = psr.SkillID
		LEFT JOIN dbo.PCQuestStatus pqs ON pqs.PlayerID = plc.PlayerID AND pqs.QuestID = pqr.RequiredQuestID
		WHERE (psr.SkillID IS NULL OR psr.SkillID = pcsk.SkillID)
			AND (psr.RequiredRank IS NULL OR psr.RequiredRank <= pcsk.Rank)
			AND (pqr.RequiredQuestID IS NULL OR (pqr.RequiredQuestID = pqs.QuestID AND pqs.CompletionDate IS NOT NULL))
			AND pc.IsActive = 1
	)
	ORDER BY pc.Sequence

END

