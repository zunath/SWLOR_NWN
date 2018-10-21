-- =============================================
-- Author:		zunath
-- Create date: 2018-08-11
-- Description:	Returns the craft blueprints available to PC
--				based on skill and perk levels.
-- =============================================
CREATE PROCEDURE dbo.GetPCBlueprintsByCategoryID
	@PlayerID NVARCHAR(60),
	@CraftCategoryID INT

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT cb2.CraftBlueprintID ,
           cb2.CraftCategoryID ,
           cb2.BaseLevel ,
           cb2.ItemName ,
           cb2.ItemResref ,
           cb2.Quantity ,
           cb2.SkillID ,
           cb2.CraftDeviceID ,
           cb2.PerkID ,
           cb2.RequiredPerkLevel ,
           cb2.IsActive ,
           cb2.MainComponentTypeID ,
           cb2.MainMinimum ,
           cb2.SecondaryComponentTypeID ,
           cb2.SecondaryMinimum ,
           cb2.TertiaryComponentTypeID ,
           cb2.TertiaryMinimum ,
           cb2.EnhancementSlots,
		   cb2.MainMaximum,
		   cb2.SecondaryMaximum,
		   cb2.TertiaryMaximum,
		   cb2.BaseStructureID
	FROM dbo.CraftBlueprints AS cb2
	JOIN dbo.PCSkills pcs ON pcs.PlayerID = @PlayerID AND cb2.SkillID = pcs.SkillID
	LEFT JOIN dbo.PCPerks pcp ON (cb2.PerkID IS NULL OR pcp.PerkID = cb2.PerkID)
		AND (pcp.PerkLevel >= cb2.RequiredPerkLevel)
		AND (pcs.PlayerID = pcp.PlayerID)
	WHERE cb2.IsActive = 1
		AND (cb2.BaseLevel <= pcs.Rank+5)
		AND (pcp.PCPerkID IS NOT NULL OR cb2.PerkID IS NULL)
		AND cb2.CraftCategoryID = @CraftCategoryID
END






