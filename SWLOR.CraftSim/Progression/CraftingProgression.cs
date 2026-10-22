using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.Game.Server.Service;
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

        /// <param name="cells">The legacy craft results for the skill and gear level, one per recipe level 1 to 49 (keyed by level).</param>
        /// <param name="plan">Guild hand-ins made along the way, or null for none.</param>
        public static SkillPath Simulate(
            SkillType skill,
            string gear,
            IReadOnlyDictionary<int, CraftCell> cells,
            RecipeCatalog recipes,
            ComponentSupply supply,
            ProgressionAssumptions assumptions,
            GuildHandInPlan plan = null)
        {
            ServerXp.ForDelta(0); // runs the XP chart's startup so GetDeltaXP has its cap
            if (plan != null)
                Guild.LoadData(); // sets Guild.MaxRank

            var recipesByLevel = recipes.ForSkill(skill).GroupBy(r => r.Level).ToDictionary(g => g.Key, g => g.ToList());
            var demand = new Dictionary<int, LevelDemand>();
            var varietyBonusLeft = new Dictionary<int, double>();
            var steps = new List<RankStep>();
            var handInTiers = plan?.TierByRecipeLevel;
            var craftedByHand = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var guildRank = 0;
            var guildPoints = 0.0;

            for (var rank = 0; rank <= HighestLevel; rank++)
            {
                var level = Math.Max(1, rank);
                if (!cells.TryGetValue(level, out var cell))
                    throw new InvalidDataException($"No {skill} {gear} craft results for recipe level {level}.");

                if (!demand.TryGetValue(level, out var levelDemand))
                {
                    if (!recipesByLevel.TryGetValue(level, out var atLevel))
                        throw new InvalidDataException($"{skill} has no recipe at level {level}.");

                    demand[level] = levelDemand = LevelDemand.Build(atLevel, supply, assumptions);
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
                var consumed = success + LegacyCraftRules.FailureLossPercent / 100.0 * (1 - success);
                var craftSeconds = cell.ActionsPerCraft * assumptions.SecondsPerAction + assumptions.OverheadSecondsPerCraft;
                var gatherSeconds = levelDemand.SecondsPerAttempt * consumed;

                var required = Math.Min(Skill.GetRequiredXP(rank), assumptions.RequiredXpCap ?? int.MaxValue);
                var remaining = (double)required;
                double crafts = 0, handIns = 0, guildXp = 0, handInSeconds = 0, credits = 0;
                double catchUpHandIns = 0, catchUpSeconds = 0;

                var handInTier = handInTiers != null && handInTiers.TryGetValue(level, out var tier) ? tier : (int?)null;
                if (plan != null && plan.QuickCraftCatchUp && handInTier.HasValue && !plan.IgnoreGuildRank)
                {
                    while (guildRank < handInTier.Value && guildRank < Guild.MaxRank)
                    {
                        var eligible = plan.CraftedTasks
                            .Where(t => t.Tier == guildRank && t.Level.Value + QuickCraftRules.RankMargin <= rank)
                            .ToList();
                        if (eligible.Count == 0)
                            break;

                        var task = eligible
                            .OrderBy(t => RankingSeconds(t.Recipe, supply, assumptions))
                            .ThenBy(t => t.QuestId, StringComparer.Ordinal)
                            .First();
                        var (taskCredits, basePoints) = plan.RewardsByTier[guildRank];
                        var pointsEach = GuildHandInPlan.GuildPointsFor(basePoints, guildRank);
                        var handInsNeeded = Math.Max(0, Guild.GetGPRequiredForRank(guildRank) - guildPoints) / pointsEach;

                        // The quick craft window needs the recipe made by hand once.
                        var seconds = 0.0;
                        if (craftedByHand.Add(task.Resref))
                        {
                            var handCell = cells[task.Level.Value];
                            seconds += handCell.ActionsPerCraft * assumptions.SecondsPerAction + assumptions.OverheadSecondsPerCraft +
                                       LevelDemand.SecondsOf(task.Recipe, supply, assumptions);
                        }

                        seconds += handInsNeeded *
                                   (QuickCraftBatch.SecondsPerItem + assumptions.HandInSeconds +
                                    LevelDemand.SecondsOf(task.Recipe, supply, assumptions));
                        catchUpSeconds += seconds;
                        catchUpHandIns += handInsNeeded;
                        credits += handInsNeeded * taskCredits;
                        guildPoints = Guild.GetGPRequiredForRank(guildRank);
                        guildRank++;
                    }
                }

                while (remaining > 1e-9)
                {
                    var bonusSuccesses = Math.Min(success, varietyBonusLeft[level]);
                    var craftXp = bonusSuccesses * firstXp + (success - bonusSuccesses) * normalXp + (1 - success) * failureXp;

                    var handIn = handInTier.HasValue && (plan.IgnoreGuildRank || guildRank >= handInTier.Value);
                    var handInXp = handIn ? success * plan.XpFraction * baseXp * multiplier : 0;
                    var xp = craftXp + handInXp;
                    if (xp <= 0)
                        throw new InvalidOperationException($"{skill} {gear} rank {rank} earns no XP, so the climb never ends.");

                    var share = Math.Min(1.0, remaining / xp);
                    crafts += share;
                    remaining -= xp * share;
                    varietyBonusLeft[level] -= bonusSuccesses * share;

                    if (!handIn)
                        continue;

                    var handInsNow = success * share;
                    handIns += handInsNow;
                    guildXp += handInXp * share;
                    handInSeconds += handInsNow * assumptions.HandInSeconds;
                    var (taskCredits, basePoints) = plan.RewardsByTier[handInTier.Value];
                    credits += handInsNow * taskCredits;
                    guildPoints = Math.Min(
                        guildPoints + handInsNow * GuildHandInPlan.GuildPointsFor(basePoints, guildRank),
                        Guild.GetGPRequiredForRank(Guild.MaxRank) - 1);
                    while (guildRank < Guild.MaxRank && guildPoints >= Guild.GetGPRequiredForRank(guildRank))
                        guildRank++;
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
                    crafts * gatherSeconds / 3600,
                    handInSeconds / 3600,
                    catchUpSeconds / 3600,
                    crafts * levelDemand.ModelledUnits * consumed,
                    crafts * levelDemand.UnmodelledUnits * consumed,
                    handIns,
                    catchUpHandIns,
                    guildXp,
                    credits));
            }

            return new SkillPath(skill, gear, steps, guildRank);
        }

        private static double RankingSeconds(RecipeEntry recipe, ComponentSupply supply, ProgressionAssumptions assumptions)
        {
            var ranking = assumptions with { UnmodelledUnitSeconds = RankingSecondsPerUnmodelledUnit };
            return LevelDemand.SecondsOf(recipe, supply, ranking);
        }
    }
}
