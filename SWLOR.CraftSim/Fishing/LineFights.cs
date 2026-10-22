using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>What a run of fights against one fish came to: how many were landed, snapped or escaped, and the turns they took.</summary>
    /// <param name="Runs">Fights played.</param>
    /// <param name="Landed">Fights that ended with the fish landed.</param>
    /// <param name="Snapped">Fights that ended with a snapped line.</param>
    /// <param name="Escaped">Fights that ended with an escape, including one the player stopped playing.</param>
    /// <param name="Turns">Actions over all the fights.</param>
    /// <param name="TurnCounts">How many fights took each number of actions: index n is the count of fights of n actions.</param>
    public readonly record struct LineCatchSample(int Runs, int Landed, int Snapped, int Escaped, int Turns, IReadOnlyList<int> TurnCounts = null)
    {
        public double LandedPercent => Runs == 0 ? 0 : 100.0 * Landed / Runs;
        public double SnappedPercent => Runs == 0 ? 0 : 100.0 * Snapped / Runs;
        public double MeanTurns => Runs == 0 ? 0 : (double)Turns / Runs;

        /// <summary>Two samples are equal when they hold the same counts, the counts of turns compared by value.</summary>
        public bool Equals(LineCatchSample other) =>
            Runs == other.Runs && Landed == other.Landed && Snapped == other.Snapped && Escaped == other.Escaped && Turns == other.Turns &&
            (TurnCounts == null ? other.TurnCounts == null : other.TurnCounts != null && TurnCounts.SequenceEqual(other.TurnCounts));

        public override int GetHashCode() => HashCode.Combine(Runs, Landed, Snapped, Escaped, Turns);

        /// <summary>
        /// The mean over the fights of a value that depends on how many actions the fight took. A sample without the counts uses the mean
        /// number of actions for every fight.
        /// </summary>
        public double Mean(Func<int, double> valueForTurns)
        {
            if (Runs == 0)
                return 0;

            if (TurnCounts == null)
                return valueForTurns((int)Math.Round(MeanTurns));

            double sum = 0;
            for (var turns = 0; turns < TurnCounts.Count; turns++)
                sum += TurnCounts[turns] * valueForTurns(turns);

            return sum / Runs;
        }
    }

    /// <summary>Plays fights of the Line with the simulator's players and the chance each takes from the seed.</summary>
    public static class LineFights
    {
        /// <summary>No fight takes this many actions; one that reaches it counts as an escape.</summary>
        public const int ActionLimit = 40;

        /// <summary>The adaptive player's search for the Line: the generic expectimax with the Line's own heuristic.</summary>
        public static readonly AdaptivePolicyOptions AdaptiveOptions = new()
        {
            Depth = 3,
            ForecastSamples = 2,
            Heuristic = LineLeafHeuristic.Instance
        };

        /// <summary>One fight played to its end.</summary>
        public static LineEngine Play(LineSetup setup, ICraftPolicy policy, IMinigameRandom random)
        {
            var engine = new LineEngine(random, setup);
            CraftPlayer.Play(engine, policy, ActionLimit);
            return engine;
        }

        /// <summary>
        /// <paramref name="runs"/> fights against the same fish, each with its own dice and its own player, from the seed and a stream
        /// that names the case so no two cases share dice.
        /// </summary>
        public static LineCatchSample Sample(LineSetup setup, Func<int, ICraftPolicy> policyFor, int runs, int seed, int stream)
        {
            int landed = 0, snapped = 0, escaped = 0, turns = 0;
            var turnCounts = new int[ActionLimit + 1];
            for (var run = 0; run < runs; run++)
            {
                var random = SimMinigameRandom.ForRun(seed, SimStreams.Adaptive, stream, run);
                var engine = Play(setup, policyFor(run), random);
                turns += engine.Turn;
                turnCounts[Math.Min(engine.Turn, ActionLimit)]++;
                switch (engine.Ending)
                {
                    case LineEnding.Landed:
                        landed++;
                        break;
                    case LineEnding.Snapped:
                        snapped++;
                        break;
                    default:
                        escaped++;
                        break;
                }
            }

            return new LineCatchSample(runs, landed, snapped, escaped, turns, turnCounts);
        }

        /// <summary>The adaptive player for run <paramref name="run"/>; each run searches with its own forecast samples.</summary>
        public static ICraftPolicy Adaptive(int run) => new AdaptivePolicy(AdaptiveOptions, run + 1);

        /// <summary>The rule-of-thumb player: reads the announced move and answers it by a fixed rule.</summary>
        public static ICraftPolicy RuleOfThumb(int run) => new RuleOfThumbPolicy();
    }
}
