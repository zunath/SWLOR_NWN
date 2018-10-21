-- =============================================
-- Author:		zunath
-- Create date: 2018-08-11
-- Description:	Returns perk data used for menu headers
-- =============================================
CREATE PROCEDURE dbo.GetPCPerksForMenuHeader
	@PlayerID NVARCHAR(60)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT pcp.PCPerkID,
		   p.Name AS Name,
		   pcp.PerkLevel AS Level,
		   pl.Description AS BonusDescription
	FROM dbo.PCPerks pcp
	JOIN dbo.Perks p ON p.PerkID = pcp.PerkID
	JOIN dbo.PerkLevels pl ON pl.PerkID = p.PerkID AND pl.Level = pcp.PerkLevel
	WHERE pcp.PlayerID = @PlayerID
	ORDER BY p.Name
END


