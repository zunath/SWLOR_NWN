namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// The Fabrication XP research gives. Both amounts start from the chart's XP for the blueprint's recipe level against the
/// researcher's Fabrication rank (<c>Skill.GetDeltaXP</c>), so research the researcher has outgrown gives nothing. Research gave no
/// XP before experiments. Every value is a starting value to tune, and the arithmetic is whole numbers so it rounds down the same
/// way on every machine.
/// </summary>
public static class ResearchXpRules
{
    /// <summary>The experiment's XP at full effort, as a percent of the chart XP.</summary>
    public const int ExperimentXpPercent = 100;

    /// <summary>The XP for completing the job, as a percent of the chart XP.</summary>
    public const int CompletionXpPercent = 50;

    /// <summary>The bonus, in percent, for each research level the blueprint reaches: a level 10 job pays double.</summary>
    public const int LevelBonusPercentPerLevel = 10;

    /// <summary>The chart XP for researching a recipe at a Fabrication rank. Needs the XP chart loaded, as at module start.</summary>
    public static int BaseXp(int recipeLevel, int fabricationRank) => Skill.GetDeltaXP(recipeLevel - fabricationRank);

    /// <summary>The XP for an experiment: the share of the trials the player used (<paramref name="effortPercent"/>) of the full amount.</summary>
    public static int ExperimentXp(int baseXp, int targetLevel, int effortPercent) =>
        (int)(Scale(baseXp, ExperimentXpPercent, targetLevel) * Math.Clamp(effortPercent, 0, 100) / 100);

    /// <summary>The XP for completing the job that brings a blueprint to <paramref name="level"/>.</summary>
    public static int CompletionXp(int baseXp, int level) => (int)Scale(baseXp, CompletionXpPercent, level);

    private static long Scale(int baseXp, int percent, int level) => baseXp <= 0
        ? 0
        : (long)baseXp * percent * (100 + LevelBonusPercentPerLevel * Math.Clamp(level, 1, ResearchGrantRules.MaxLevel)) / 10_000;
}
