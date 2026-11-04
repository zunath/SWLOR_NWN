using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>Which replacement for today's harvests a harvesting cohort plays.</summary>
    public enum HarvestingLoop
    {
        /// <summary>Quick Harvest: the node with no seams, today's expected units and XP in today's time.</summary>
        QuickHarvest = 1,

        /// <summary>A skilled Prospecting session: the plan with the most items per node.</summary>
        SkilledSession = 2,

        /// <summary>A casual session: Strike until the node is spent.</summary>
        StrikeOnlySession = 3
    }

    /// <summary>
    /// The harvesting cohort of the XP-rate gate, as the plan pins it: one case for each node tier, the Gathering rank equal to the
    /// vein's level (5, 15, 25, 35, 45) with the highest harvester tier that rank allows, XP per item gathered, at the attribute reference
    /// (a Might modifier of 0). Both loops spend a harvest delay for each action: today's harvest is one action of 5 seconds, and a session's
    /// actions take the seconds <c>ProspectingRules</c> names, expressed here in harvest delays so the gate's single seconds-per-action
    /// stays meaningful. A run is a node.
    /// </summary>
    public sealed class HarvestingXpRateCohort : IXpRateCohort
    {
        /// <summary>How many nodes each case stands for, so the means can be played as whole runs.</summary>
        public const int NodesPerCase = 10_000;

        private readonly HarvestingParity _parity;
        private readonly HarvestingLoop _loop;

        public HarvestingXpRateCohort(HarvestingParity parity, HarvestingLoop loop)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _loop = loop;
        }

        public string Name => _loop switch
        {
            HarvestingLoop.QuickHarvest => "Harvesting, Quick Harvest",
            HarvestingLoop.SkilledSession => "Harvesting, skilled session",
            _ => "Harvesting, strike-only session"
        };

        public string RunName => "node";

        /// <summary>Today's harvest is one action of one harvest delay, with nothing else to spend time on in the loop.</summary>
        public XpLoopTiming LegacyTiming => new(_parity.Options.SecondsPerHarvest);

        /// <summary>A harvest delay is the unit of time here; a session's actions are converted to it.</summary>
        public XpLoopTiming ReplacementTiming => new(_parity.Options.SecondsPerHarvest);

        /// <summary>The rank is the vein's level, so only the tiers whose level a rank can reach (every tier: level 45 at most) are cases.</summary>
        public IReadOnlyList<XpRateCase> Cases(GateContext context) =>
            _parity.Tiers.Select(tier => new XpRateCase(
                $"Tier {tier} vein (level {GatheringLevels.ForTier(tier)})",
                $"harvester tier {HarvestingParity.HighestHarvesterTier(RankFor(tier))}, rank = vein level")).ToList();

        public XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context)
        {
            var tier = TierOf(xpCase);
            var rank = RankFor(tier);
            return new XpLoopSample(
                NodesPerCase * HarvestModel.ExpectedXpPerNode(tier, rank, 0),
                NodesPerCase * HarvestModel.ExpectedHarvestsPerNode,
                NodesPerCase);
        }

        public XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context)
        {
            var tier = TierOf(xpCase);
            var rank = RankFor(tier);
            var harvestSeconds = _parity.Options.SecondsPerHarvest;
            switch (_loop)
            {
                case HarvestingLoop.QuickHarvest:
                    // The rule is today's: ProspectingQuickHarvest.ExpectedUnits units, a harvest delay and today's XP for each.
                    return new XpLoopSample(
                        NodesPerCase * ProspectingQuickHarvest.ExpectedUnits * HarvestingParity.QuickHarvestXpPerUnit(tier, rank),
                        NodesPerCase * ProspectingQuickHarvest.ExpectedUnits,
                        NodesPerCase);

                case HarvestingLoop.SkilledSession:
                    return Session(tier, rank, _parity.Skilled(tier, rank).Mean, harvestSeconds);

                default:
                    return Session(tier, rank, _parity.Mean(tier, rank, ProspectorPlan.Casual), harvestSeconds);
            }
        }

        private static XpLoopSample Session(int tier, int rank, ProspectingMean mean, double harvestSeconds) =>
            new(
                NodesPerCase * mean.Items * HarvestingParity.SessionXpPerItem(tier, rank),
                NodesPerCase * mean.Seconds / harvestSeconds,
                NodesPerCase);

        /// <summary>The rank of the cohort for a tier: the vein's level, at most the cap.</summary>
        private static int RankFor(int tier) => Math.Min(50, GatheringLevels.ForTier(tier));

        private static int TierOf(XpRateCase xpCase)
        {
            var open = xpCase.Group.IndexOf(' ') + 1;
            return int.Parse(xpCase.Group[open..xpCase.Group.IndexOf(' ', open)]);
        }
    }
}
