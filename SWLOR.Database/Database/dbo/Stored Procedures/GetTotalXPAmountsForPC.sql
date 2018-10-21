-- =============================================
-- Author:		zunath
-- Create date: 2018-08-10
-- Description:	Returns a result set containing the skill ID and total skill XP earned by a player.
-- =============================================
CREATE PROCEDURE dbo.GetTotalXPAmountsForPC
	@PlayerID NVARCHAR(60),
	@SkillID INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	

	SELECT pcs.SkillID, CAST(SUM(xp.XP) + pcs.XP AS INTEGER) AS TotalSkillXP
	FROM dbo.PCSkills pcs
	JOIN dbo.SkillXPRequirement xp ON xp.SkillID = pcs.SkillID AND ((xp.Rank < pcs.Rank) OR (xp.Rank = 0 AND pcs.XP > 0))
	WHERE pcs.IsLocked = 0
		AND pcs.PlayerID = @PlayerID
		AND pcs.SkillID <> @SkillID
	GROUP BY pcs.SkillID, pcs.XP
	ORDER BY pcs.SkillID

END
