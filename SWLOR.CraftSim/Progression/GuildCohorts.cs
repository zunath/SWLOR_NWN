using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>The climb of one guild rank by hand-ins of quick-crafted items, for a crafter who has reached rank 50.</summary>
    /// <param name="Tier">The guild rank being climbed from.</param>
    /// <param name="Resref">The task item the crafter prefers: the cheapest the tier offers that quick craft allows at rank 50. The hours use the expected cost of the preferred item that is on the ten-task board.</param>
    /// <param name="ItemLevel">That item's recipe level.</param>
    /// <param name="GuildPointsEach">Guild points one hand-in earns at this guild rank.</param>
    /// <param name="HandIns">Hand-ins to climb the rank.</param>
    /// <param name="Hours">Hours the hand-ins take: quick craft, turn-in and gathering the components.</param>
    /// <param name="Credits">Credits the hand-ins pay.</param>
    /// <param name="BoardChance">The chance a board holds at least one eligible item of the tier; with none, the crafter waits for the next server start (calendar time, not charged).</param>
    public sealed record GuildChoreRow(
        GuildType Guild, int Tier, string Resref, int ItemLevel, int GuildPointsEach, double HandIns, double Hours, double Credits, double BoardChance);

    /// <summary>One tier of Agriculture's catch tasks at a crafter whose rank matches the fish.</summary>
    /// <param name="Tier">The guild rank that unlocks the tier.</param>
    /// <param name="FishLevel">The median level of the tier's fish (<c>FishType</c>); the rank is set equal to it.</param>
    /// <param name="Quantity">Fish a task asks for.</param>
    /// <param name="CatchChance">Chance a bite lands the fish at this rank and Willpower.</param>
    /// <param name="BitesPerTask">Bites to land the fish a task asks for.</param>
    /// <param name="BiteXp">XP each bite pays whether or not the fish is landed (<c>GetDeltaXP</c> of the fish level minus the rank).</param>
    /// <param name="SecondsPerTask">Seconds for the bites and the turn-in.</param>
    public sealed record CatchTaskRow(
        int Tier, int FishLevel, int Quantity, double CatchChance, double BitesPerTask, int BiteXp, double SecondsPerTask)
    {
        public double BiteXpPerTask => BitesPerTask * BiteXp;

        /// <summary>Hand-in XP at an award share: one base XP per task, or one per fish when it scales with quantity.</summary>
        public double HandInXp(double share, bool scaledByQuantity) => share * BiteXp * (scaledByQuantity ? Quantity : 1);

        public double XpPerHour(double handInXp) => (BiteXpPerTask + handInXp) / SecondsPerTask * 3600;
    }

    /// <summary>The crafting-guild hand-in cohorts that the progression model reports beside the crafting climb.</summary>
    public static class GuildCohorts
    {
        /// <summary>The highest recipe level a rank-50 crafter may quick craft (rank at least level + <see cref="QuickCraftRules.RankMargin"/>).</summary>
        public static int HighestQuickCraftLevel => ProgressionAssumptions.MaximumRank - QuickCraftRules.RankMargin;

        /// <summary>
        /// What raising the guild rank costs a rank-50 crafter who quick-crafts the cheapest eligible task item of each tier and hands it
        /// in: no minigame, no XP, the components and the turn-in time. The task cap used to allow ten tasks a tier per server start.
        /// </summary>
        public static IReadOnlyList<GuildChoreRow> Chore(
            GuildTaskCatalog guilds, ComponentSupply supply, GuildType guild, ProgressionAssumptions assumptions)
        {
            Guild.LoadData();
            var rows = new List<GuildChoreRow>();
            var tasks = guilds.For(guild).Where(t => t.Kind == GuildTaskKind.Crafted).ToList();
            var previousThreshold = 0;
            for (var tier = 0; tier < Guild.MaxRank; tier++)
            {
                var eligible = tasks
                    .Where(t => t.Tier == tier && t.Level.Value <= HighestQuickCraftLevel)
                    .OrderBy(t => LevelDemand.SecondsOf(t.Recipe, supply, assumptions with { UnmodelledUnitSeconds = 20 }))
                    .ThenBy(t => t.QuestId, StringComparer.Ordinal)
                    .ToList();
                if (eligible.Count == 0)
                    throw new InvalidDataException($"{guild} tier {tier} has no task item a rank-50 crafter can quick craft.");

                // The board keeps ten random tasks of the tier, so the crafter makes the cheapest eligible one that is on it.
                var perItem = eligible
                    .Select(t => QuickCraftBatch.SecondsPerItem + assumptions.HandInSeconds + LevelDemand.SecondsOf(t.Recipe, supply, assumptions))
                    .ToList();
                var tierTasks = guilds.For(guild).Count(t => t.Tier == tier);
                var (boardChance, itemSeconds) = GuildBoard.PreferredOnBoard(perItem, tierTasks);
                var task = eligible[0];
                var pointsEach = GuildHandInPlan.GuildPointsFor(task.GuildPoints, tier);
                var threshold = Guild.GetGPRequiredForRank(tier);
                var handIns = (threshold - previousThreshold) / (double)pointsEach;
                rows.Add(new GuildChoreRow(guild, tier, task.Resref, task.Level.Value, pointsEach, handIns, handIns * itemSeconds / 3600, handIns * task.Credits, boardChance));
                previousThreshold = threshold;
            }

            return rows;
        }

        /// <summary>Agriculture's catch tasks, one row per tier, at a crafter whose Agriculture rank equals the tier's median fish level.</summary>
        public static IReadOnlyList<CatchTaskRow> CatchTasks(GuildTaskCatalog guilds, ProgressionAssumptions assumptions)
        {
            ServerXp.ForDelta(0);
            var rows = new List<CatchTaskRow>();
            var caught = guilds.For(GuildType.AgricultureGuild).Where(t => t.Kind == GuildTaskKind.Caught).ToList();
            foreach (var tier in caught.Select(t => t.Tier).Distinct().OrderBy(t => t))
            {
                var tasks = caught.Where(t => t.Tier == tier).OrderBy(t => t.Level.Value).ToList();
                var level = tasks[tasks.Count / 2].Level.Value;
                var quantity = tasks[tasks.Count / 2].Quantity;
                var rank = Math.Min(level, ProgressionAssumptions.MaximumRank);
                var chance = FishingCatchModel.CatchChancePercent(rank, level, assumptions.WillpowerScore) / 100.0;
                var bites = quantity / chance;
                rows.Add(new CatchTaskRow(
                    tier,
                    level,
                    quantity,
                    chance,
                    bites,
                    ServerXp.ForDelta(level - rank),
                    bites * assumptions.FishingSecondsPerBite + assumptions.HandInSeconds));
            }

            return rows;
        }
    }
}
