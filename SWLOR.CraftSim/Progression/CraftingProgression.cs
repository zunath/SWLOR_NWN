using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// Plays a new crafter from rank 0 to rank 50 with expected values. At every rank the crafter makes recipes of their own level (at
    /// least level 1), pays the XP chart's price for them, and climbs when the rank's required XP is in. Each craft costs its actions and
    /// overhead in the craft window and the gathering time of the components it consumes, failures included.
    /// </summary>
    public static class CraftingProgression
    {
        /// <summary>Highest recipe level a rank 0-49 crafter works at.</summary>
        public const int HighestLevel = ProgressionAssumptions.MaximumRank - 1;

        /// <summary>
        /// When ranking which task item is cheapest to make, a component no node supplies counts as this many seconds (about what a
        /// harvested unit costs), so recipes full of drops are not mistaken for cheap ones. It never changes the hours reported.
        /// </summary>
        private const double RankingSecondsPerUnmodelledUnit = 20.0;

        /// <summary>A recipe the model gives a success chance below this is treated as having it, so the attempts to a first success stay finite.</summary>
        public const double MinimumSuccessRate = 0.02;

        /// <param name="results">The legacy craft results for the skill and gear level, by recipe level and rank.</param>
        /// <param name="plan">Guild hand-ins made along the way, or null for none.</param>
        public static SkillPath Simulate(
            SkillType skill,
            string gear,
            ICraftResults results,
            RecipeCatalog recipes,
            ComponentSupply supply,
            ProgressionAssumptions assumptions,
            GuildHandInPlan plan = null)
        {
            ServerXp.ForDelta(0); // runs the XP chart's startup so GetDeltaXP has its cap
            if (plan != null)
                Guild.LoadData(); // sets Guild.MaxRank

            var recipesByLevel = recipes.ForNewCrafter(skill).GroupBy(r => r.Level).ToDictionary(g => g.Key, g => g.ToList());
            var demand = new Dictionary<int, LevelDemand>();
            var varietyBonusLeft = new Dictionary<int, double>();
            var steps = new List<RankStep>();
            var offersByLevel = plan?.OffersByRecipeLevel;
            var manualCraftTiers = new HashSet<int>();
            var guildRank = 0;
            var guildPoints = 0.0;
            var lowestCatchUpChance = 1.0;
            var restartsWaited = 0.0;

            for (var rank = 0; rank <= HighestLevel; rank++)
            {
                var level = Math.Max(1, rank);

                // The crafter gathers their own components with a Gathering rank that follows their crafting rank, which sets Hard Look at
                // scavenge sites.
                var gathering = assumptions with { GatheringTier = CraftRankMilestones.TierForRank(rank) };

                // Rank 0 crafts the level-1 recipe at rank 0, which is not the result of the same recipe at rank 1: CP, progress and the actions
                // unlocked all depend on the rank.
                var cell = results.At(level, rank);

                if (!demand.TryGetValue(level, out var levelDemand))
                {
                    if (!recipesByLevel.TryGetValue(level, out var atLevel))
                        throw new InvalidDataException($"{skill} has no recipe at level {level}.");

                    demand[level] = levelDemand = LevelDemand.Build(atLevel, supply, gathering);
                    varietyBonusLeft[level] = assumptions.VarietyPlay ? atLevel.Count : 0;
                }

                var success = cell.SuccessRate;
                var quality = cell.QualityFraction;
                var baseXp = ServerXp.ForDelta(level - rank);
                var multiplier = assumptions.CraftXpMultiplier;
                var firstXp = CraftOutcomeRules.CalculateXp(baseXp, 0, true, (float)quality) * multiplier;
                var normalXp = CraftOutcomeRules.CalculateXp(baseXp, 0, false, (float)quality) * multiplier;
                var failureXp = CraftOutcomeRules.CalculateFailureXp(baseXp, 0) * multiplier;

                // A failed craft loses each stack on 65% of rolls, so a craft consumes this share of the committed stacks.
                var consumed = ConsumedShare(success);
                var craftSeconds = cell.ActionsPerCraft * assumptions.SecondsPerAction + assumptions.OverheadSecondsPerCraft;

                var required = Math.Min(Skill.GetRequiredXP(rank), assumptions.RequiredXpCap ?? int.MaxValue);
                var remaining = (double)required;
                double crafts = 0, handIns = 0, guildXp = 0, handInSeconds = 0, credits = 0;
                double catchUpHandIns = 0, catchUpSeconds = 0, lockedCrafts = 0;
                double gatherSecondsTotal = 0, modelledUnitsTotal = 0, unmodelledUnitsTotal = 0;

                // The tiers that ask for a recipe of this level. Tasks of several tiers can ask for the same recipe.
                IReadOnlyList<LevelOffer> offers = null;
                if (offersByLevel != null)
                    offersByLevel.TryGetValue(level, out offers);

                if (plan != null && plan.QuickCraftCatchUp && offers != null && !plan.IgnoreGuildRank)
                {
                    var lowestTier = offers.Min(o => o.Tier);
                    while (guildRank < lowestTier && guildRank < Guild.MaxRank)
                    {
                        var eligible = plan.CraftedTasks
                            .Where(t => t.Tier == guildRank && t.Level.Value + QuickCraftRules.RankMargin <= rank)
                            .OrderBy(t => RankingSeconds(t.Recipe, supply, gathering))
                            .ThenBy(t => t.QuestId, StringComparer.Ordinal)
                            .ToList();
                        if (eligible.Count == 0)
                            break;

                        // The board holds ten random tasks of the tier, so the crafter takes the preferred eligible one that is on it.
                        var gather = eligible.Select(t => LevelDemand.SecondsOf(t.Recipe, supply, gathering)).ToList();
                        var perItem = gather.Select(g => QuickCraftBatch.SecondsPerItem + assumptions.HandInSeconds + g).ToList();
                        var tierTasks = plan.TasksPerTier[guildRank];
                        var (onBoard, itemSeconds) = GuildBoard.PreferredOnBoard(perItem, tierTasks);
                        if (onBoard <= 0)
                            break;

                        // A board without an eligible item makes the crafter wait for the next server start. The wait is not play time, but the
                        // crafts made meanwhile cannot be handed in, so they are made without the hand-in award: the expected number of boards that
                        // miss before one hits is (1 - chance) / chance, each lasting the assumed play hours.
                        lowestCatchUpChance = Math.Min(lowestCatchUpChance, onBoard);
                        var missedBoards = (1 - onBoard) / onBoard;
                        restartsWaited += missedBoards;
                        var secondsPerCraft = craftSeconds + levelDemand.SecondsPerAttempt * consumed;
                        lockedCrafts += missedBoards * assumptions.PlayHoursPerServerStart * 3600 / secondsPerCraft;

                        var (taskCredits, basePoints) = plan.RewardsByTier[guildRank];
                        var pointsEach = GuildHandInPlan.GuildPointsFor(basePoints, guildRank);
                        var handInsNeeded = Math.Max(0, Guild.GetGPRequiredForRank(guildRank) - guildPoints) / pointsEach;

                        // The quick craft window needs the recipe made by hand once, successfully: a failed attempt is made again, and loses
                        // its components on the usual share of rolls, so the cost is the expected attempts to a first success.
                        var seconds = handInsNeeded * itemSeconds;
                        if (manualCraftTiers.Add(guildRank))
                        {
                            var byHand = eligible
                                .Select((t, index) =>
                                {
                                    var attemptCell = results.At(t.Level.Value, rank);
                                    var attempts = 1 / Math.Max(attemptCell.SuccessRate, MinimumSuccessRate);
                                    return attempts * (attemptCell.ActionsPerCraft * assumptions.SecondsPerAction + assumptions.OverheadSecondsPerCraft +
                                                       ConsumedShare(attemptCell.SuccessRate) * gather[index]);
                                })
                                .ToList();
                            seconds += GuildBoard.PreferredOnBoard(byHand, tierTasks).ExpectedCost;
                        }

                        catchUpSeconds += seconds;
                        catchUpHandIns += handInsNeeded;
                        credits += handInsNeeded * taskCredits;
                        guildPoints = Guild.GetGPRequiredForRank(guildRank);
                        guildRank++;
                    }
                }

                var taskMixes = new Dictionary<string, TaskMix>();
                while (remaining > 1e-9)
                {
                    var bonusSuccesses = Math.Min(success, varietyBonusLeft[level]);
                    var craftXp = bonusSuccesses * firstXp + (success - bonusSuccesses) * normalXp + (1 - success) * failureXp;

                    // A hand-in needs a task for the level on a board the guild rank unlocks (each tier has its own board), and the crafts made
                    // while a catch-up waits for a board cannot be handed in.
                    var locked = lockedCrafts > 1e-12;
                    var odds = offers == null || locked ? HandInOdds.None : HandInOdds.For(plan.Unlocked(offers, guildRank));
                    var handIn = odds.Chance > 0;
                    var handInChance = success * odds.Chance;
                    var handInXp = plan == null ? 0 : handInChance * plan.XpFraction * baseXp * multiplier;
                    var xp = craftXp + handInXp;
                    if (xp <= 0)
                        throw new InvalidOperationException($"{skill} {gear} rank {rank} earns no XP, so the climb never ends.");

                    var share = Math.Min(1.0, remaining / xp);
                    if (locked)
                        share = Math.Min(share, lockedCrafts);

                    crafts += share;
                    remaining -= xp * share;
                    lockedCrafts = Math.Max(0, lockedCrafts - (locked ? share : 0));
                    varietyBonusLeft[level] -= bonusSuccesses * share;

                    // A hand-in asks for one exact recipe (the quest collector matches the resref), so when a board holds a task for the level
                    // the crafter makes that recipe, and its components, not the level's average, are what the craft costs.
                    var taskShare = odds.Chance;
                    var mix = handIn ? TaskMix.For(taskMixes, plan, odds, level, supply, gathering) : null;
                    gatherSecondsTotal += share * consumed * ((1 - taskShare) * levelDemand.SecondsPerAttempt + taskShare * (mix?.Seconds ?? 0));
                    modelledUnitsTotal += share * consumed * ((1 - taskShare) * levelDemand.ModelledUnits + taskShare * (mix?.ModelledUnits ?? 0));
                    unmodelledUnitsTotal += share * consumed * ((1 - taskShare) * levelDemand.UnmodelledUnits + taskShare * (mix?.UnmodelledUnits ?? 0));

                    if (!handIn)
                        continue;

                    var handInsNow = handInChance * share;
                    handIns += handInsNow;
                    guildXp += handInXp * share;
                    handInSeconds += handInsNow * assumptions.HandInSeconds;
                    credits += handInsNow * plan.ExpectedCredits(odds);
                    (guildRank, guildPoints) = GuildHandInPlan.AddHandIns(guildRank, guildPoints, handInsNow, atRank => plan.ExpectedGuildPoints(odds, atRank));
                }

                steps.Add(new RankStep(
                    rank,
                    level,
                    success,
                    quality,
                    cell.ActionsPerCraft,
                    baseXp,
                    required,
                    crafts,
                    crafts * craftSeconds / 3600,
                    gatherSecondsTotal / 3600,
                    handInSeconds / 3600,
                    catchUpSeconds / 3600,
                    modelledUnitsTotal,
                    unmodelledUnitsTotal,
                    handIns,
                    catchUpHandIns,
                    guildXp,
                    credits));
            }

            return new SkillPath(skill, gear, steps, guildRank, lowestCatchUpChance, restartsWaited);
        }

        public static double ConsumedShare(double success) =>
            success + LegacyCraftRules.FailureLossPercent / 100.0 * (1 - success);

        /// <summary>What a craft costs when it is a guild task recipe: the components of the level's tasks that a crafter at the guild rank may hand in.</summary>
        private sealed class TaskMix
        {
            public double Seconds { get; private init; }
            public double ModelledUnits { get; private init; }
            public double UnmodelledUnits { get; private init; }

            /// <summary>
            /// The crafter makes whichever of the level's tasks is on a board, without picking the cheapest to gather (a crafter who picks
            /// recipes by cost can do so with or without a guild, and the sensitivity table prices that separately). Every task of the level in
            /// the tiers handed in to is equally likely to be on its board, so the expected cost of the one made is the mean over those tasks.
            /// </summary>
            public static TaskMix For(
                Dictionary<string, TaskMix> cache, GuildHandInPlan plan, HandInOdds odds, int level, ComponentSupply supply, ProgressionAssumptions assumptions)
            {
                var key = string.Join(",", odds.Tiers.Select(t => t.Tier));
                if (cache.TryGetValue(key, out var found))
                    return found;

                var tiers = odds.Tiers.Select(t => t.Tier).ToHashSet();
                var tasks = plan.CraftedTasks.Where(t => t.Level.Value == level && tiers.Contains(t.Tier)).ToList();
                return cache[key] = new TaskMix
                {
                    Seconds = tasks.Average(t => LevelDemand.SecondsOf(t.Recipe, supply, assumptions)),
                    ModelledUnits = tasks.Average(t => t.Recipe.Components.Where(c => supply.IsModelled(c.Key)).Sum(c => (double)c.Value)),
                    UnmodelledUnits = tasks.Average(t => t.Recipe.Components.Where(c => !supply.IsModelled(c.Key)).Sum(c => (double)c.Value))
                };
            }
        }

        private static double RankingSeconds(RecipeEntry recipe, ComponentSupply supply, ProgressionAssumptions assumptions)
        {
            var ranking = assumptions with { UnmodelledUnitSeconds = RankingSecondsPerUnmodelledUnit };
            return LevelDemand.SecondsOf(recipe, supply, ranking);
        }
    }
}
