using System.Collections.Concurrent;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>One case of the study: a node tier, a Gathering rank and whether the node can hold gems.</summary>
    public readonly record struct HarvestCase(int Tier, int Rank, bool HasGems);

    /// <summary>What the study plays and how it judges: the starting values the plan leaves to the simulator. Each is recorded in the report's inputs.</summary>
    public sealed record HarvestingParityOptions
    {
        public static readonly HarvestingParityOptions Default = new();

        /// <summary>The Gathering ranks the yield table plays at each tier, from the rank that unlocks the tier.</summary>
        public IReadOnlyList<int> Ranks { get; init; } = Enumerable.Range(0, 11).Select(step => step * 5).ToList();

        /// <summary>A skilled session may yield this much more than today's node, as a share of today's (the plan's +15%).</summary>
        public double SkilledCapShare { get; init; } = 0.15;

        /// <summary>
        /// A skilled session must yield at least this share of today's, so playing is never worse than Quick Harvest. The small allowance
        /// is the sampling noise of the played sessions.
        /// </summary>
        public double SkilledFloorShare { get; init; } = 0.99;

        /// <summary>The fewest actions per node the skilled player averages (the plan's length target for a node).</summary>
        public double LeastActions { get; init; } = 3;

        /// <summary>The most actions per node the skilled player averages.</summary>
        public double MostActions { get; init; } = 8;

        /// <summary>The seconds one harvest takes today and in Quick Harvest: the harvester's delay.</summary>
        public double SecondsPerHarvest { get; init; } = ProspectingRules.HarvestSeconds;

        /// <summary>The charges of the harvester the sessions are played with.</summary>
        public int Charges { get; init; } = ProspectingPlays.FreshHarvesterCharges;
    }

    /// <summary>The means of a run of nodes, weighted over the kinds of node of a tier.</summary>
    public readonly record struct ProspectingMean(
        double Items,
        double Units,
        double RichRolls,
        double Gems,
        double Actions,
        double Seconds,
        double Charges,
        double Collapsed,
        double ShareWithinLength)
    {
        public static ProspectingMean Of(ProspectingSample sample, double least, double most) => new(
            sample.ItemsPerNode,
            sample.UnitsPerNode,
            sample.RichRollsPerNode,
            sample.GemsPerNode,
            sample.ActionsPerNode,
            sample.SecondsPerNode,
            sample.ChargesPerNode,
            sample.CollapsedShare,
            sample.ShareWithActions((int)Math.Ceiling(least), (int)Math.Floor(most)));

        public static ProspectingMean Blend(ProspectingMean first, double firstWeight, ProspectingMean second)
        {
            var secondWeight = 1 - firstWeight;
            return new ProspectingMean(
                first.Items * firstWeight + second.Items * secondWeight,
                first.Units * firstWeight + second.Units * secondWeight,
                first.RichRolls * firstWeight + second.RichRolls * secondWeight,
                first.Gems * firstWeight + second.Gems * secondWeight,
                first.Actions * firstWeight + second.Actions * secondWeight,
                first.Seconds * firstWeight + second.Seconds * secondWeight,
                first.Charges * firstWeight + second.Charges * secondWeight,
                first.Collapsed * firstWeight + second.Collapsed * secondWeight,
                first.ShareWithinLength * firstWeight + second.ShareWithinLength * secondWeight);
        }
    }

    /// <summary>The skilled player's best plan at a tier and rank and what it came to.</summary>
    public readonly record struct SkilledPlay(ProspectorPlan Plan, ProspectingMean Mean);

    /// <summary>
    /// Plays Prospecting against the nodes of the game and remembers each case, so the tables of one report share their sessions. Every
    /// case takes its dice from its own stream of the seed, so a table repeats exactly whatever order the cases ran in. A tier's nodes are
    /// weighted by the kinds the module has: the share whose loot table holds a gem can hold gem pockets, the rest cannot.
    /// </summary>
    public sealed class HarvestingParity
    {
        private readonly ConcurrentDictionary<(HarvestCase Case, ProspectorPlan Plan), ProspectingSample> _samples = new();
        private readonly ConcurrentDictionary<HarvestCase, ProspectingSample> _quick = new();
        private readonly IReadOnlyList<HarvestNodeType> _nodes;
        private readonly HarvestingParityOptions _options;

        public HarvestingParity(int runs, int seed, IReadOnlyList<HarvestNodeType> nodes, HarvestingParityOptions options = null)
        {
            if (runs < 1)
                throw new ArgumentOutOfRangeException(nameof(runs), "A case needs at least one session.");

            Runs = runs;
            Seed = seed;
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _options = options ?? HarvestingParityOptions.Default;
        }

        public int Runs { get; }

        public int Seed { get; }

        public HarvestingParityOptions Options => _options;

        public IReadOnlyList<HarvestNodeType> Nodes => _nodes;

        /// <summary>The tiers the module has nodes of.</summary>
        public IReadOnlyList<int> Tiers => _nodes.Select(node => node.Tier).Distinct().OrderBy(tier => tier).ToList();

        /// <summary>The Gathering ranks a node of the tier can be worked at: from the rank that unlocks its harvester up to the cap.</summary>
        public IEnumerable<int> RanksFor(int tier) =>
            _options.Ranks.Where(rank => rank >= CraftRankMilestones.RequiredRankForTier(tier));

        public ProspectingSample Play(HarvestCase harvestCase, ProspectorPlan plan) =>
            _samples.GetOrAdd((harvestCase, plan), key => ProspectingPlays.Sample(
                key.Case.Tier,
                key.Case.Rank,
                key.Case.HasGems,
                key.Plan,
                Runs,
                Seed,
                ProspectingPlays.Stream(key.Case.Tier, key.Case.Rank, key.Case.HasGems, key.Plan),
                _options.Charges));

        public ProspectingSample QuickHarvestSample(HarvestCase harvestCase) =>
            _quick.GetOrAdd(harvestCase, key => ProspectingPlays.QuickHarvest(
                key.Tier, key.Rank, key.HasGems, Runs, Seed, ProspectingPlays.Stream(key.Tier, key.Rank, key.HasGems, ProspectorPlan.Casual), _options.Charges));

        /// <summary>The share of the tier's nodes that can hold gem pockets: those whose loot table has a gem entry.</summary>
        public double GemShare(int tier)
        {
            var ofTier = _nodes.Where(node => node.Tier == tier).ToList();
            return ofTier.Count == 0 ? 0 : (double)ofTier.Count(node => node.HasGems) / ofTier.Count;
        }

        /// <summary>The mean of a plan over the tier's kinds of node.</summary>
        public ProspectingMean Mean(int tier, int rank, ProspectorPlan plan) =>
            Blend(tier, hasGems => ProspectingMean.Of(Play(new HarvestCase(tier, rank, hasGems), plan), _options.LeastActions, _options.MostActions));

        /// <summary>The mean of Quick Harvest over the tier's kinds of node.</summary>
        public ProspectingMean QuickHarvest(int tier, int rank) =>
            Blend(tier, hasGems => ProspectingMean.Of(QuickHarvestSample(new HarvestCase(tier, rank, hasGems)), 0, 0));

        /// <summary>The skilled player's best plan: the one with the most items per node, the fewer seconds winning a tie.</summary>
        public SkilledPlay Skilled(int tier, int rank)
        {
            SkilledPlay? best = null;
            foreach (var plan in ProspectorPlan.Skilled)
            {
                var mean = Mean(tier, rank, plan);
                if (best == null ||
                    mean.Items > best.Value.Mean.Items + 1e-9 ||
                    Math.Abs(mean.Items - best.Value.Mean.Items) <= 1e-9 && mean.Seconds < best.Value.Mean.Seconds)
                {
                    best = new SkilledPlay(plan, mean);
                }
            }

            return best!.Value;
        }

        /// <summary>
        /// The most items per node any plan reaches, the careful and the fast ones: the ceiling the +15% cap is checked against. It is the
        /// highest of a number of plans each played from its own dice, so it sits a little above the true mean of the best plan.
        /// </summary>
        public SkilledPlay Ceiling(int tier, int rank)
        {
            var best = Skilled(tier, rank);
            foreach (var plan in ProspectorPlan.Fast.Concat(ProspectorPlan.Cautious))
            {
                var mean = Mean(tier, rank, plan);
                if (mean.Items > best.Mean.Items)
                    best = new SkilledPlay(plan, mean);
            }

            return best;
        }

        /// <summary>
        /// Today's gems per node at a tier: 2.5 harvests, each a roll of the table, so the tier's mean share of a roll that is a gem times
        /// the units of a node.
        /// </summary>
        public double TodayGemsPerNode(int tier)
        {
            var ofTier = _nodes.Where(node => node.Tier == tier).ToList();
            return ofTier.Count == 0 ? 0 : ofTier.Average(node => node.GemChance) * TodayItemsPerNode;
        }

        /// <summary>Plays every case in parallel, so the tables that follow only read.</summary>
        public void Prepare()
        {
            var cases = new List<HarvestCase>();
            foreach (var tier in Tiers)
            {
                foreach (var rank in RanksFor(tier))
                {
                    cases.Add(new HarvestCase(tier, rank, true));
                    if (GemShare(tier) < 1)
                        cases.Add(new HarvestCase(tier, rank, false));
                }
            }

            Parallel.ForEach(cases, harvestCase =>
            {
                foreach (var plan in ProspectorPlan.All.Append(ProspectorPlan.Reckless))
                    Play(harvestCase, plan);

                QuickHarvestSample(harvestCase);
            });
        }

        /// <summary>Today's items per node at the attribute reference (a Might modifier of 0): the mean of the d4 harvests.</summary>
        public static double TodayItemsPerNode => HarvestModel.ExpectedItemsPerNode(0);

        /// <summary>Today's XP per item at a tier and rank.</summary>
        public static int TodayXpPerItem(int tier, int rank) => HarvestModel.XpPerItem(tier, rank);

        /// <summary>The XP one item pays a session at a tier and rank: today's XP times the minigame's multiplier for the vein's level.</summary>
        public static int SessionXpPerItem(int tier, int rank) =>
            CraftMinigameXp.ScaleBaseXp(
                ServerXp.ForDelta(GatheringLevels.ForTier(tier) - rank),
                CraftMinigameXp.MultiplierFor(CraftMinigameType.Prospecting, GatheringLevels.ForTier(tier)));

        /// <summary>The XP one unit pays Quick Harvest at a tier and rank: today's, with no multiplier.</summary>
        public static int QuickHarvestXpPerUnit(int tier, int rank) => ServerXp.ForDelta(GatheringLevels.ForTier(tier) - rank);

        /// <summary>The highest harvester tier a rank allows.</summary>
        public static int HighestHarvesterTier(int rank) => CraftRankMilestones.TierForRank(rank);

        private ProspectingMean Blend(int tier, Func<bool, ProspectingMean> meanFor)
        {
            var share = GemShare(tier);
            if (share >= 1)
                return meanFor(true);

            if (share <= 0)
                return meanFor(false);

            return ProspectingMean.Blend(meanFor(true), share, meanFor(false));
        }
    }
}
