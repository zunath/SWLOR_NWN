namespace SWLOR.CraftSim.Progression
{
    /// <summary>What it takes to climb one rank (from <see cref="Rank"/> to the next) in a skill.</summary>
    /// <param name="Rank">The rank the crafter starts the step at, 0 to 49.</param>
    /// <param name="RecipeLevel">The level of the recipes crafted: the crafter's own rank, at least 1.</param>
    /// <param name="SuccessRate">Share of crafts that finish the item.</param>
    /// <param name="QualityFraction">Mean final quality of the successes, 0 to 1.</param>
    /// <param name="ActionsPerCraft">Mean actions per craft, failures included.</param>
    /// <param name="BaseXp">The XP chart's pay for a craft at this recipe level and rank, before quality and the first-craft bonus.</param>
    /// <param name="RequiredXp">XP the step needs: <c>Skill.GetRequiredXP(rank)</c>.</param>
    /// <param name="Crafts">Crafts the step takes, fractional because the model works with expected values.</param>
    /// <param name="CraftHours">Hours spent in the craft window: actions at the per-action time plus the per-craft overhead.</param>
    /// <param name="GatherHours">Hours gathering and refining the components the crafts consume.</param>
    /// <param name="HandInHours">Hours spent handing at-level crafts in to the guild.</param>
    /// <param name="CatchUpHours">Hours spent making and handing in cheap lower-level task items to raise the guild rank (gathering included).</param>
    /// <param name="ModelledUnits">Component units a node supplies that the crafts consume (failures included).</param>
    /// <param name="UnmodelledUnits">Component units no node supplies that the crafts consume.</param>
    /// <param name="HandIns">Guild hand-ins of at-level crafts made during the step.</param>
    /// <param name="CatchUpHandIns">Guild hand-ins of quick-crafted lower-level items made during the step.</param>
    /// <param name="GuildXp">XP the at-level hand-ins paid.</param>
    /// <param name="Credits">Credits the hand-ins paid.</param>
    public sealed record RankStep(
        int Rank,
        int RecipeLevel,
        double SuccessRate,
        double QualityFraction,
        double ActionsPerCraft,
        int BaseXp,
        int RequiredXp,
        double Crafts,
        double CraftHours,
        double GatherHours,
        double HandInHours,
        double CatchUpHours,
        double ModelledUnits,
        double UnmodelledUnits,
        double HandIns,
        double CatchUpHandIns,
        double GuildXp,
        double Credits)
    {
        public double TotalHours => CraftHours + GatherHours + HandInHours + CatchUpHours;
    }
}
