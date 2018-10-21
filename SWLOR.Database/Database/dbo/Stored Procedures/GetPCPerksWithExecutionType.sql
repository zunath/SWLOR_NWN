-- =============================================
-- Author:		zunath
-- Create date: 2018-08-11
-- Description:	Returns all PCPerks for a given player ID which also have
--				execution types.
-- =============================================
CREATE PROCEDURE dbo.GetPCPerksWithExecutionType
	@PlayerID NVARCHAR(60)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT pcp.PCPerkID ,
		   pcp.PlayerID ,
		   pcp.AcquiredDate ,
		   pcp.PerkID ,
		   pcp.PerkLevel
	FROM dbo.PCPerks pcp
	CROSS APPLY dbo.fn_GetPlayerEffectivePerkLevel(pcp.PlayerID, pcp.PerkID, NULL) ap
	JOIN dbo.Perks p ON p.PerkID = pcp.PerkID
	WHERE pcp.PlayerID = @PlayerID
		AND p.ExecutionTypeID > 0

END

