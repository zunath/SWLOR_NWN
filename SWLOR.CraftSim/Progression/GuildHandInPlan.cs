using SWLOR.Game.Server.Enumeration;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>The guild's offer for crafts of one recipe level: the tier that unlocks it and how likely this boot's board is to hold one.</summary>
    /// <param name="Tier">The lowest guild rank that unlocks a task for a recipe of the level.</param>
    /// <param name="TasksAtLevel">Tasks of that tier asking for a recipe of the level.</param>
    /// <param name="TasksInTier">All the tasks of that tier, of which the board keeps ten.</param>
    public readonly record struct LevelOffer(int Tier, int TasksAtLevel, int TasksInTier)
    {
        /// <summary>The chance the board holds at least one task for the level (<see cref="GuildBoard.ChanceAtLeastOne"/>).</summary>
        public double ChanceOnBoard => GuildBoard.ChanceAtLeastOne(TasksInTier, TasksAtLevel);
    }

    /// <summary>
    /// A crafter who hands an at-level craft in to the guild whenever this boot's board has a task for that recipe level and the guild rank
    /// allows it, and who raises the guild rank when it stands in the way. Guild points follow <c>Guild.CalculateGPReward</c> with no perks
    /// and no Social bonus, and the guild rank follows the server's thresholds. The board keeps ten random tasks per tier
    /// (<see cref="GuildBoard"/>), so a level's task is on it with a chance, not always.
    /// </summary>
    /// <param name="Guild">The guild of the skill being climbed.</param>
    /// <param name="CraftedTasks">The guild's crafted-item tasks.</param>
    /// <param name="TasksPerTier">All the guild's tasks per tier (crafted and caught), of which the board keeps ten.</param>
    /// <param name="RewardsByTier">Credits and base guild points per task, by tier.</param>
    /// <param name="XpFraction">Share of the craft's base XP (<c>GetDeltaXP</c> of the recipe level minus the rank) each hand-in pays. Zero pays no XP.</param>
    /// <param name="IgnoreGuildRank">When true every task is open from the start, the upper bound on what the hand-ins can pay.</param>
    /// <param name="QuickCraftCatchUp">
    /// When true and the at-level task is locked by guild rank, the crafter quick-crafts the preferred task item of their current guild
    /// tier that is on the board and that quick craft allows (rank at least level + 5, after one craft by hand) and hands it in until the
    /// guild rank rises. The preferred item is the one cheapest to gather.
    /// </param>
    public sealed record GuildHandInPlan(
        GuildType Guild,
        IReadOnlyList<GuildTask> CraftedTasks,
        IReadOnlyDictionary<int, int> TasksPerTier,
        IReadOnlyDictionary<int, (int Credits, int GuildPoints)> RewardsByTier,
        double XpFraction,
        bool IgnoreGuildRank,
        bool QuickCraftCatchUp)
    {
        /// <summary>The guild rank bonus on a task's guild points: 25% per rank (<c>Guild.CalculateGPReward</c>).</summary>
        public const float RankBonusPerRank = 0.25f;

        /// <summary>For each recipe level a crafted task exists for, the lowest tier that asks for it and how many of that tier's tasks do.</summary>
        public IReadOnlyDictionary<int, LevelOffer> OfferByRecipeLevel =>
            CraftedTasks
                .GroupBy(t => t.Level.Value)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var tier = g.Min(t => t.Tier);
                        return new LevelOffer(tier, g.Count(t => t.Tier == tier), TasksPerTier[tier]);
                    });

        /// <summary>Guild points one hand-in earns at a guild rank, before perks and Social.</summary>
        public static int GuildPointsFor(int basePoints, int guildRank) =>
            (int)(basePoints + RankBonusPerRank * guildRank * basePoints);

        /// <summary>
        /// Adds hand-ins to a guild's points. Each earns guild points at the rank the guild has when it is made, and the rank rises the moment the
        /// points reach the threshold (<c>Guild.GiveGuildPoints</c>), so a batch is split at every threshold it crosses. Needs <c>Guild.LoadData</c>.
        /// </summary>
        public static (int GuildRank, double Points) AddHandIns(int guildRank, double points, double handIns, int basePoints)
        {
            var maximumPoints = Game.Server.Service.Guild.GetGPRequiredForRank(Game.Server.Service.Guild.MaxRank) - 1;
            while (handIns > 1e-12)
            {
                if (guildRank >= Game.Server.Service.Guild.MaxRank)
                    return (guildRank, Math.Min(points, maximumPoints));

                var pointsEach = GuildPointsFor(basePoints, guildRank);
                var needed = Game.Server.Service.Guild.GetGPRequiredForRank(guildRank) - points;
                if (handIns * pointsEach < needed)
                    return (guildRank, points + handIns * pointsEach);

                handIns -= needed / pointsEach;
                points = Game.Server.Service.Guild.GetGPRequiredForRank(guildRank);
                guildRank++;
            }

            return (guildRank, Math.Min(points, maximumPoints));
        }

        public static GuildHandInPlan For(
            GuildTaskCatalog catalog, GuildType guild, double xpFraction, bool ignoreGuildRank = false, bool quickCraftCatchUp = true)
        {
            var tiers = catalog.For(guild).Select(t => t.Tier).Distinct().OrderBy(t => t).ToList();
            return new GuildHandInPlan(
                guild,
                catalog.For(guild).Where(t => t.Kind == GuildTaskKind.Crafted).ToList(),
                tiers.ToDictionary(tier => tier, tier => catalog.For(guild).Count(t => t.Tier == tier)),
                tiers.ToDictionary(tier => tier, tier => catalog.RewardOfTier(guild, tier)),
                xpFraction,
                ignoreGuildRank,
                quickCraftCatchUp);
        }
    }
}
