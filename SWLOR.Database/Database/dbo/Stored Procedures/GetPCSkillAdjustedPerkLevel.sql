-- =============================================
-- Author:		zunath
-- Create date: 2018-08-11
-- Description:	Returns player's skill-adjusted perk level.
-- =============================================
CREATE PROCEDURE dbo.GetPCSkillAdjustedPerkLevel
	@PlayerID NVARCHAR(60),
	@PerkID INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT PerkLevelID ,
       PerkID ,
       Level ,
       Price ,
       Description
	FROM dbo.fn_GetPlayerEffectivePerkLevel(@PlayerID, @PerkID, NULL)
END
