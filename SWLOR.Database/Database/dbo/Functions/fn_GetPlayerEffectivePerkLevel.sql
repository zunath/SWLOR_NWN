CREATE FUNCTION dbo.fn_GetPlayerEffectivePerkLevel
(	
	@PlayerID NVARCHAR(60),
	@PerkID INT,
	@SkillLevel INT
)
RETURNS TABLE 
AS
RETURN 
(
	SELECT pl.PerkLevelID ,
		   pl.PerkID ,
		   pl.Level ,
		   pl.Price ,
		   pl.Description
	FROM dbo.PerkLevels pl
	WHERE pl.PerkLevelID = (
		SELECT TOP(1) pl2.PerkLevelID
		FROM dbo.PerkLevels pl2
		LEFT JOIN dbo.PerkLevelSkillRequirements pr ON pr.PerkLevelID = pl2.PerkLevelID
		JOIN dbo.PCPerks pcp ON pcp.PerkID = pl2.PerkID
			AND pcp.PlayerID = @PlayerID
			AND pcp.PerkLevel >= pl2.Level
		JOIN dbo.PCSkills pcs ON pcs.PlayerID = pcp.PlayerID
			AND pcs.SkillID = ISNULL(pr.SkillID, pcs.SkillID)
		WHERE pcp.PerkID = @PerkID
			AND ISNULL(@SkillLevel, pcs.Rank) >= ISNULL(pr.RequiredRank, 0)
		ORDER BY pl2.Level DESC

	)
)


