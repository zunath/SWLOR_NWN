-- =============================================
-- Author:		zunath
-- Create date: 2018-08-11
-- Description:	Returns crafting categories available for player based on
--				perk and skill levels.
-- =============================================
CREATE PROCEDURE dbo.GetCategoriesAvailableToPC
	@PlayerID NVARCHAR(60)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT cbc.CraftBlueprintCategoryID ,
       cbc.Name ,
       cbc.IsActive
	FROM dbo.CraftBlueprintCategories cbc
	WHERE cbc.CraftBlueprintCategoryID IN (
		SELECT DISTINCT cb2.CraftCategoryID
		FROM dbo.CraftBlueprints AS cb2
		JOIN dbo.PCSkills pcs ON pcs.PlayerID = @PlayerID
		LEFT JOIN dbo.PCPerks pcp ON (cb2.PerkID IS NULL OR pcp.PerkID = cb2.PerkID)
			AND (pcp.PerkLevel >= cb2.RequiredPerkLevel)
			AND (pcs.PlayerID = pcp.PlayerID)
		WHERE cb2.IsActive = 1
			AND (cb2.BaseLevel <= pcs.Rank+5)
			AND (pcp.PCPerkID IS NOT NULL OR cb2.PerkID IS NULL)
	)
		AND cbc.IsActive = 1
	ORDER BY cbc.Name ASC
END


