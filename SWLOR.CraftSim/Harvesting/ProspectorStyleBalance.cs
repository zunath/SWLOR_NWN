using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>
    /// Gate 2 for Prospecting, the style balance: a gatherer's style is a trade, so it must be the better choice on enough nodes and not
    /// lead on average. A node of the game is one of nine kinds (a pool of 1 to 4 units and 1 to 3 seams, no more than the pool holds); the
    /// styles are played on the same nodes and dice, each by the best of the skilled player's plans for it, and scored by items per second
    /// of play including the seconds it takes to reach the next node. Each kind of node at each Gathering rank is one case, a point is one
    /// percent of the better style's score, and the gate's thresholds are the crafting gates' (each style the better choice on at least
    /// 30% of the cases, a mean lead of at most 5 points, a tie within half a point splitting the win).
    /// </summary>
    public static class ProspectorStyleBalance
    {
        /// <summary>The stream that separates the study's dice from every gate's.</summary>
        public const int StudyStream = 211;

        /// <summary>The Gathering ranks the styles are compared at: a style opens at 20.</summary>
        public static readonly IReadOnlyList<int> Ranks = new[] { 20, 25, 30, 35, 40, 45, 50 };

        /// <summary>The kinds of node: a pool of units and the seams sealed in it.</summary>
        public static readonly IReadOnlyList<(int Pool, int Seams)> Kinds = new[]
        {
            (1, 1), (2, 1), (2, 2), (3, 1), (3, 2), (3, 3), (4, 1), (4, 2), (4, 3)
        };

        /// <summary>What one style came to on one kind of node: the means over the nodes of that kind.</summary>
        public readonly record struct Score(int Nodes, double Items, double Seconds)
        {
            public double Rate(double overheadSeconds) => Nodes == 0 ? 0 : Items / (Seconds + overheadSeconds);
        }

        /// <summary>
        /// Plays <paramref name="runs"/> nodes with each style's effects at the rank and returns, for each kind of node and style, the
        /// score of the skilled plan that does best for the kind.
        /// </summary>
        public static (IReadOnlyDictionary<(int Pool, int Seams), Score> First, IReadOnlyDictionary<(int Pool, int Seams), Score> Second) Play(
            int rank,
            double gemChance,
            int runs,
            int seed,
            ProspectingEffects first,
            ProspectingEffects second,
            double overheadSeconds)
        {
            var one = new Dictionary<(int, int), Dictionary<ProspectorPlan, (int Nodes, double Items, double Seconds)>>();
            var two = new Dictionary<(int, int), Dictionary<ProspectorPlan, (int Nodes, double Items, double Seconds)>>();
            for (var run = 0; run < runs; run++)
            {
                var nodeRandom = SimMinigameRandom.ForRun(seed, StudyStream, rank, 0, run);
                var node = ProspectingNode.Create(nodeRandom, gemChance);
                var kind = (node.Pool, node.Seams.Length);
                foreach (var plan in ProspectorPlan.Skilled)
                {
                    Accumulate(one, kind, plan, PlayNode(node, rank, first, plan, seed, run));
                    Accumulate(two, kind, plan, PlayNode(node, rank, second, plan, seed, run));
                }
            }

            return (Best(one, overheadSeconds), Best(two, overheadSeconds));
        }

        private static (double Items, double Seconds) PlayNode(ProspectingNode node, int rank, ProspectingEffects effects, ProspectorPlan plan, int seed, int run)
        {
            var random = SimMinigameRandom.ForRun(seed, StudyStream, rank, 1, run);
            var engine = new ProspectingEngine(random, ProspectingSetup.ForNode(node, ProspectingPlays.FreshHarvesterCharges, rank, 3, effects));
            var policy = new ProspectorPolicy(plan);
            double seconds = 0;
            var played = 0;
            while (engine.Status == MinigameStatus.InProgress && played < ProspectingPlays.ActionLimit)
            {
                var action = policy.Choose(engine);
                if (action == null)
                    break;

                var actionSeconds = engine.SecondsFor(action);
                if (!engine.Perform(action).Performed)
                    break;

                if (action != ProspectingActionIds.Leave)
                {
                    played++;
                    seconds += actionSeconds;
                }
            }

            return (engine.Items, seconds);
        }

        private static void Accumulate(
            Dictionary<(int, int), Dictionary<ProspectorPlan, (int Nodes, double Items, double Seconds)>> sums,
            (int, int) kind,
            ProspectorPlan plan,
            (double Items, double Seconds) result)
        {
            if (!sums.TryGetValue(kind, out var plans))
                sums[kind] = plans = new Dictionary<ProspectorPlan, (int, double, double)>();

            plans.TryGetValue(plan, out var current);
            plans[plan] = (current.Nodes + 1, current.Items + result.Items, current.Seconds + result.Seconds);
        }

        private static IReadOnlyDictionary<(int Pool, int Seams), Score> Best(
            Dictionary<(int, int), Dictionary<ProspectorPlan, (int Nodes, double Items, double Seconds)>> sums,
            double overheadSeconds)
        {
            var best = new Dictionary<(int Pool, int Seams), Score>();
            foreach (var (kind, plans) in sums)
            {
                Score? top = null;
                foreach (var (nodes, items, seconds) in plans.Values)
                {
                    var score = new Score(nodes, items / nodes, seconds / nodes);
                    if (top == null || score.Rate(overheadSeconds) > top.Value.Rate(overheadSeconds))
                        top = score;
                }

                best[kind] = top!.Value;
            }

            return best;
        }

        /// <summary>
        /// One comparison of two styles over the kinds of node, at one rank or (<see cref="Rank"/> 0) pooled over every rank, which is the
        /// row the gate judges: a style is chosen once for a gatherer's career at the ranks it is open at, as the crafting gates pool the
        /// recipes of a level kind. The rows of single ranks are evidence of how the balance moves.
        /// </summary>
        public sealed record Row(int Rank, int Cases, double FirstWins, double SecondWins, double MeanGap, double WeightedGap, bool Passed)
        {
            public bool IsGate => Rank == 0;
        }

        /// <summary>Compares two styles at the ranks and returns a row for each, with the cases counted by kind of node.</summary>
        public static IReadOnlyList<Row> Compare(
            Func<int, ProspectingEffects> first,
            Func<int, ProspectingEffects> second,
            double gemChance,
            int runs,
            int seed,
            GateThresholds thresholds,
            double overheadSeconds,
            int minimumNodes = 40)
        {
            var rows = new List<Row>();
            double pooledFirst = 0, pooledSecond = 0, pooledGap = 0, pooledWeighted = 0, pooledWeights = 0;
            var pooledCases = 0;
            foreach (var rank in Ranks)
            {
                var (a, b) = Play(rank, gemChance, runs, seed, first(rank), second(rank), overheadSeconds);
                double firstWins = 0, secondWins = 0, gap = 0, weighted = 0, weights = 0;
                var cases = 0;
                foreach (var kind in Kinds)
                {
                    if (!a.TryGetValue(kind, out var left) || !b.TryGetValue(kind, out var right) || left.Nodes < minimumNodes)
                        continue;

                    var leftRate = left.Rate(overheadSeconds);
                    var rightRate = right.Rate(overheadSeconds);
                    var best = Math.Max(leftRate, rightRate);
                    var difference = best <= 0 ? 0 : 100 * (leftRate - rightRate) / best;
                    if (Math.Abs(difference) <= thresholds.StyleTieMarginPoints)
                    {
                        firstWins += 0.5;
                        secondWins += 0.5;
                    }
                    else if (difference > 0)
                    {
                        firstWins++;
                    }
                    else
                    {
                        secondWins++;
                    }

                    gap += difference;
                    weighted += difference * left.Nodes;
                    weights += left.Nodes;
                    cases++;
                }

                pooledFirst += firstWins;
                pooledSecond += secondWins;
                pooledGap += gap;
                pooledWeighted += weighted;
                pooledWeights += weights;
                pooledCases += cases;

                var firstShare = cases == 0 ? 0 : firstWins / cases;
                var secondShare = cases == 0 ? 0 : secondWins / cases;
                var meanGap = cases == 0 ? 0 : gap / cases;
                var passed = firstShare >= thresholds.StyleMinimumWinShare &&
                             secondShare >= thresholds.StyleMinimumWinShare &&
                             Math.Abs(meanGap) <= thresholds.StyleMaximumMeanDifferencePoints;
                rows.Add(new Row(rank, cases, firstShare, secondShare, meanGap, weights == 0 ? 0 : weighted / weights, passed));
            }

            var allFirst = pooledCases == 0 ? 0 : pooledFirst / pooledCases;
            var allSecond = pooledCases == 0 ? 0 : pooledSecond / pooledCases;
            var allGap = pooledCases == 0 ? 0 : pooledGap / pooledCases;
            rows.Add(new Row(
                0,
                pooledCases,
                allFirst,
                allSecond,
                allGap,
                pooledWeights == 0 ? 0 : pooledWeighted / pooledWeights,
                allFirst >= thresholds.StyleMinimumWinShare && allSecond >= thresholds.StyleMinimumWinShare &&
                Math.Abs(allGap) <= thresholds.StyleMaximumMeanDifferencePoints));
            return rows;
        }

        public static GateTable Table(string title, string firstName, string secondName, IReadOnlyList<Row> rows)
        {
            var table = new EvidenceTable(
                "Gathering rank", "Kinds of node", $"{firstName} better", $"{secondName} better",
                $"Mean lead of {firstName} (points)", "Same, weighted by how often the kind occurs", "Verdict");
            foreach (var row in rows)
            {
                table.Add(
                    row.IsGate ? "All ranks (the gate)" : row.Rank.ToString(),
                    row.Cases.ToString(),
                    Percent(row.FirstWins, 0),
                    Percent(row.SecondWins, 0),
                    Number(row.MeanGap, 1),
                    Number(row.WeightedGap, 1),
                    row.IsGate ? (row.Passed ? "Passed" : "Failed") : (row.Passed ? "Within" : "Outside"));
            }

            return table.ToGateTable(title);
        }

        /// <summary>The seconds from one node to the next, the same for both styles.</summary>
        public static double OverheadSeconds => new ProgressionAssumptions().NodeTravelSeconds;
    }
}
