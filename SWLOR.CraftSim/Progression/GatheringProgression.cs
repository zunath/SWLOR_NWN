using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>How a gatherer earns Gathering XP.</summary>
    public enum GatheringStrategy
    {
        /// <summary>Harvest the best ore vein the harvester tier allows, then refine the raw ore.</summary>
        HarvestAndRefine,

        /// <summary>Search the best scavenge site the Gathering tier allows.</summary>
        Scavenge,

        /// <summary>At each rank, whichever of the two pays more XP per second.</summary>
        Best
    }

    /// <summary>One rank of the Gathering climb.</summary>
    /// <param name="Activity">What the gatherer does at this rank, for the report.</param>
    /// <param name="XpPerSecond">Gathering XP per second of that activity.</param>
    public sealed record GatheringStep(int Rank, int Tier, string Activity, double XpPerSecond, int RequiredXp)
    {
        public double Hours => RequiredXp / XpPerSecond / 3600;
    }

    /// <summary>A new gatherer's climb from Gathering rank 0 to rank 50.</summary>
    public sealed class GatheringPath
    {
        public GatheringPath(GatheringStrategy strategy, IReadOnlyList<GatheringStep> steps)
        {
            Strategy = strategy;
            Steps = steps;
        }

        public GatheringStrategy Strategy { get; }
        public IReadOnlyList<GatheringStep> Steps { get; }
        public int RequiredXp => Steps.Sum(s => s.RequiredXp);
        public double TotalHours => Steps.Sum(s => s.Hours);
        public double XpPerHour => RequiredXp / TotalHours;

        public IEnumerable<GatheringStep> Band(int firstRank, int endRank) => Steps.Where(s => s.Rank >= firstRank && s.Rank < endRank);
    }

    /// <summary>
    /// Plays a new gatherer from rank 0 to rank 50 with expected values, using today's harvest, refining and scavenging rules. Harvesting
    /// pays <c>GetDeltaXP</c> of the vein's level minus the rank for every item gathered, so a vein two tiers down pays nothing; refining pays a
    /// flat XP per raw unit whatever the rank; a scavenge site pays by its level. The harvester, refinery and scavenge tiers are the
    /// Gathering rank's tier (<see cref="CraftRankMilestones.TierForRank"/>).
    /// </summary>
    public static class GatheringProgression
    {
        public static GatheringPath Simulate(GatheringStrategy strategy, ComponentSupply supply, ProgressionAssumptions assumptions)
        {
            ServerXp.ForDelta(0);
            var steps = new List<GatheringStep>();
            for (var rank = 0; rank < ProgressionAssumptions.MaximumRank; rank++)
            {
                var options = new List<(string Activity, double XpPerSecond)>();
                if (strategy is GatheringStrategy.HarvestAndRefine or GatheringStrategy.Best)
                    options.Add(BestVein(rank, supply, assumptions));
                if (strategy is GatheringStrategy.Scavenge or GatheringStrategy.Best)
                    options.Add(BestSite(rank, assumptions));

                var (activity, xpPerSecond) = options.OrderByDescending(o => o.XpPerSecond).First();
                if (xpPerSecond <= 0)
                    throw new InvalidOperationException($"Gathering rank {rank} earns no XP with {strategy}, so the climb never ends.");

                steps.Add(new GatheringStep(rank, CraftRankMilestones.TierForRank(rank), activity, xpPerSecond, Skill.GetRequiredXP(rank)));
            }

            return new GatheringPath(strategy, steps);
        }

        /// <summary>The ground vein that pays the most XP per second at a rank, refining its raw ore.</summary>
        private static (string Activity, double XpPerSecond) BestVein(int rank, ComponentSupply supply, ProgressionAssumptions assumptions)
        {
            var tier = CraftRankMilestones.TierForRank(rank);
            var itemsPerUse = HarvestModel.ExpectedItemsPerHarvest(assumptions.MightModifier);
            var best = (Activity: "no vein", XpPerSecond: 0.0);

            foreach (var ore in RefiningModel.Ores().Where(o => o.Ore.StartsWith("raw_", StringComparison.Ordinal) && o.RequiredLevel <= tier))
            {
                var node = supply.NodeFor(ore.Ore);
                if (node == null)
                    continue;

                var rawUnitsPerUse = node.Value.Share * itemsPerUse;
                var harvestXp = HarvestModel.XpPerItem(ore.RequiredLevel, rank) * itemsPerUse;
                var refineXp = rawUnitsPerUse * ore.XpPerUnit;
                var seconds = ComponentSupply.SecondsPerHarvestUse(assumptions) +
                              rawUnitsPerUse * assumptions.RefineSecondsPerBatch / assumptions.RefineBatchUnits;
                var xpPerSecond = (harvestXp + refineXp) / seconds;
                if (xpPerSecond > best.XpPerSecond)
                    best = ($"harvest and refine {ore.Ore} (tier {ore.RequiredLevel})", xpPerSecond);
            }

            return best;
        }

        /// <summary>The scavenge site level that pays the most XP per second at a rank.</summary>
        private static (string Activity, double XpPerSecond) BestSite(int rank, ProgressionAssumptions assumptions)
        {
            var tier = CraftRankMilestones.TierForRank(rank);
            var best = (Activity: "no site", XpPerSecond: 0.0);
            for (var level = ScavengeModel.FirstSiteLevel; level <= Math.Min(tier, ScavengeModel.LastSiteLevel); level++)
            {
                var xp = ScavengeModel.ExpectedXpPerSite(level, rank, tier, assumptions.PerceptionModifier);
                var attempts = ScavengeModel.ExpectedAttemptsPerSite(tier, assumptions.PerceptionModifier);
                var seconds = assumptions.NodeTravelSeconds + attempts * assumptions.SecondsPerScavengeAttempt;
                if (xp / seconds > best.XpPerSecond)
                    best = ($"scavenge level {level} sites", xp / seconds);
            }

            return best;
        }
    }
}
