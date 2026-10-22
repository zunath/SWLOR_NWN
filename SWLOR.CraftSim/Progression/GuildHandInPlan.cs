using SWLOR.Game.Server.Enumeration;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// A crafter who hands an at-level craft in to the guild whenever the guild offers a task for that recipe level, and who raises the
    /// guild rank when it stands in the way. Guild points follow <c>Guild.CalculateGPReward</c> with no perks and no Social bonus, and the
    /// guild rank follows the server's thresholds.
    /// </summary>
    /// <param name="Guild">The guild of the skill being climbed.</param>
    /// <param name="CraftedTasks">The guild's crafted-item tasks.</param>
    /// <param name="RewardsByTier">Credits and base guild points per task, by tier.</param>
    /// <param name="XpFraction">Share of the craft's base XP (<c>GetDeltaXP</c> of the recipe level minus the rank) each hand-in pays. Zero pays no XP.</param>
    /// <param name="IgnoreGuildRank">When true every task is open from the start, the upper bound on what the hand-ins can pay.</param>
    /// <param name="QuickCraftCatchUp">
    /// When true and the at-level task is locked by guild rank, the crafter quick-crafts the cheapest task item of their current guild
    /// tier that quick craft allows (rank at least level + 5, after one craft by hand) and hands it in until the guild rank rises.
    /// </param>
    public sealed record GuildHandInPlan(
        GuildType Guild,
        IReadOnlyList<GuildTask> CraftedTasks,
        IReadOnlyDictionary<int, (int Credits, int GuildPoints)> RewardsByTier,
        double XpFraction,
        bool IgnoreGuildRank,
        bool QuickCraftCatchUp)
    {
        /// <summary>The guild rank bonus on a task's guild points: 25% per rank (<c>Guild.CalculateGPReward</c>).</summary>
        public const float RankBonusPerRank = 0.25f;

        /// <summary>For each recipe level a crafted task exists for, the guild rank that unlocks it.</summary>
        public IReadOnlyDictionary<int, int> TierByRecipeLevel =>
            CraftedTasks.GroupBy(t => t.Level.Value).ToDictionary(g => g.Key, g => g.Min(t => t.Tier));

        /// <summary>Guild points one hand-in earns at a guild rank, before perks and Social.</summary>
        public static int GuildPointsFor(int basePoints, int guildRank) =>
            (int)(basePoints + RankBonusPerRank * guildRank * basePoints);

        public static GuildHandInPlan For(
            GuildTaskCatalog catalog, GuildType guild, double xpFraction, bool ignoreGuildRank = false, bool quickCraftCatchUp = true)
        {
            var tiers = catalog.For(guild).Select(t => t.Tier).Distinct().OrderBy(t => t).ToList();
            return new GuildHandInPlan(
                guild,
                catalog.For(guild).Where(t => t.Kind == GuildTaskKind.Crafted).ToList(),
                tiers.ToDictionary(tier => tier, tier => catalog.RewardOfTier(guild, tier)),
                xpFraction,
                ignoreGuildRank,
                quickCraftCatchUp);
        }
    }
}
