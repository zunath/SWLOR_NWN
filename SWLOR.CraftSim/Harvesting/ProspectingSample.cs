using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>What a run of sessions at one kind of node came to, per node: the items gathered, the time and the charges they took.</summary>
    /// <param name="Runs">Nodes worked.</param>
    /// <param name="Items">Items gathered over all the runs: pool units, extra ore rolls and gems.</param>
    /// <param name="Units">Pool units over all the runs.</param>
    /// <param name="RichRolls">Extra ore rolls from rich seams over all the runs.</param>
    /// <param name="Gems">Gems from gem pockets over all the runs.</param>
    /// <param name="Actions">Actions taken over all the runs. Leave is not an action a player spends time on.</param>
    /// <param name="Seconds">Seconds the actions took over all the runs.</param>
    /// <param name="Charges">Harvester charges spent over all the runs.</param>
    /// <param name="Collapsed">Sessions that ended with the node collapsing.</param>
    /// <param name="MinedOut">Sessions that ended with the last unit taken.</param>
    /// <param name="PoolLeft">Units left in the nodes when the sessions ended, over all the runs.</param>
    /// <param name="ActionCounts">How many sessions took each number of actions: index n is the count of sessions of n actions.</param>
    public readonly record struct ProspectingSample(
        int Runs,
        double Items,
        double Units,
        double RichRolls,
        double Gems,
        double Actions,
        double Seconds,
        double Charges,
        int Collapsed,
        int MinedOut,
        double PoolLeft,
        IReadOnlyList<int> ActionCounts)
    {
        public double ItemsPerNode => Runs == 0 ? 0 : Items / Runs;
        public double UnitsPerNode => Runs == 0 ? 0 : Units / Runs;
        public double RichRollsPerNode => Runs == 0 ? 0 : RichRolls / Runs;
        public double GemsPerNode => Runs == 0 ? 0 : Gems / Runs;
        public double ActionsPerNode => Runs == 0 ? 0 : Actions / Runs;
        public double SecondsPerNode => Runs == 0 ? 0 : Seconds / Runs;
        public double ChargesPerNode => Runs == 0 ? 0 : Charges / Runs;
        public double CollapsedShare => Runs == 0 ? 0 : (double)Collapsed / Runs;
        public double PoolLeftPerNode => Runs == 0 ? 0 : PoolLeft / Runs;

        /// <summary>The share of sessions whose action count is within the range, both ends included.</summary>
        public double ShareWithActions(int least, int most)
        {
            if (Runs == 0 || ActionCounts == null)
                return 0;

            var within = 0;
            for (var count = least; count <= most && count < ActionCounts.Count; count++)
                within += ActionCounts[count];

            return (double)within / Runs;
        }
    }

    /// <summary>Plays Prospecting sessions with the simulator's players and the chance each takes from the seed.</summary>
    public static class ProspectingPlays
    {
        /// <summary>No session takes this many actions; one that reaches it is cut off.</summary>
        public const int ActionLimit = 40;

        /// <summary>The charges of a fresh harvester: all of them are the session's budget.</summary>
        public const int FreshHarvesterCharges = 50;

        /// <summary>
        /// <paramref name="runs"/> nodes of the tier worked by a player of the Gathering rank, each node and session with its own dice from
        /// the seed and the stream that names the case.
        /// </summary>
        public static ProspectingSample Sample(
            int nodeTier,
            int rank,
            bool hasGems,
            ProspectorPlan plan,
            int runs,
            int seed,
            int stream,
            int charges = FreshHarvesterCharges)
        {
            double items = 0, units = 0, rich = 0, gems = 0, actions = 0, seconds = 0, spent = 0, poolLeft = 0;
            int collapsed = 0, minedOut = 0;
            var counts = new int[ActionLimit + 1];
            for (var run = 0; run < runs; run++)
            {
                var random = SimMinigameRandom.ForRun(seed, SimStreams.Adaptive, stream, run);
                var node = ProspectingNode.Create(random, hasGems);
                var engine = new ProspectingEngine(random, ProspectingSetup.ForNode(node, charges, rank, nodeTier));
                var played = 0;
                double time = 0;
                var policy = new ProspectorPolicy(plan);
                while (engine.Status == MinigameStatus.InProgress && played < ActionLimit)
                {
                    var action = policy.Choose(engine);
                    if (action == null)
                        break;

                    var result = engine.Perform(action);
                    if (!result.Performed)
                        break;

                    if (action != ProspectingActionIds.Leave)
                    {
                        played++;
                        time += ProspectingRules.SecondsFor(action);
                    }
                }

                items += engine.Items;
                units += engine.Units;
                rich += engine.RichRolls;
                gems += engine.Gems;
                actions += played;
                seconds += time;
                spent += engine.ChargesAtStart - engine.Charges;
                poolLeft += engine.Pool;
                counts[played]++;
                if (engine.Ending == ProspectingEnding.Collapsed)
                    collapsed++;
                else if (engine.Ending == ProspectingEnding.MinedOut)
                    minedOut++;
            }

            return new ProspectingSample(runs, items, units, rich, gems, actions, seconds, spent, collapsed, minedOut, poolLeft, counts);
        }

        /// <summary>
        /// <paramref name="runs"/> nodes worked with Quick Harvest, through the same rule the game uses
        /// (<see cref="ProspectingQuickHarvest"/>): each takes everything its pool holds as far as the charges go, a charge and a harvest
        /// delay for each unit. Each unit is one action of <see cref="ProspectingRules.HarvestSeconds"/>.
        /// </summary>
        public static ProspectingSample QuickHarvest(int nodeTier, int rank, bool hasGems, int runs, int seed, int stream, int charges = FreshHarvesterCharges)
        {
            double items = 0, actions = 0, seconds = 0, spent = 0, poolLeft = 0;
            int minedOut = 0;
            var counts = new int[ActionLimit + 1];
            for (var run = 0; run < runs; run++)
            {
                var random = SimMinigameRandom.ForRun(seed, SimStreams.Adaptive, stream, run);
                var node = ProspectingNode.Create(random, hasGems);
                var units = ProspectingQuickHarvest.Units(node, charges);
                items += units;
                actions += units;
                seconds += ProspectingQuickHarvest.Seconds(units);
                spent += ProspectingQuickHarvest.Charges(units);
                poolLeft += node.Pool - units;
                counts[Math.Min(units, ActionLimit)]++;
                if (units == node.Pool)
                    minedOut++;
            }

            return new ProspectingSample(runs, items, items, 0, 0, actions, seconds, spent, 0, minedOut, poolLeft, counts);
        }

        /// <summary>A number that names the case, the same on every machine: FNV-1a over the case's numbers and the plan's name.</summary>
        public static int Stream(int nodeTier, int rank, bool hasGems, ProspectorPlan plan)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var part in new[] { nodeTier, rank, hasGems ? 1 : 0, plan.MaxSurveys, plan.ExtractSeams ? 1 : 0, plan.HeavyStrikes ? 1 : 0, (int)plan.Reserve })
                {
                    for (var shift = 0; shift < 32; shift += 8)
                        hash = (hash ^ (uint)((part >> shift) & 0xFF)) * 16777619u;
                }

                return (int)hash;
            }
        }
    }
}
