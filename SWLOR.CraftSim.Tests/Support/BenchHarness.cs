using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Support
{
    /// <summary>Plays back a fixed list of D100 rolls, then fails loudly; the same idea as the game server tests' scripted random.</summary>
    internal sealed class ScriptedRandom : IMinigameRandom
    {
        private readonly Queue<int> _rolls;

        public ScriptedRandom(params int[] rolls)
        {
            _rolls = new Queue<int>(rolls);
        }

        public int Calls { get; private set; }

        public int D100()
        {
            Calls++;
            return _rolls.Count > 0 ? _rolls.Dequeue() : throw new InvalidOperationException("The test did not script a roll for this call.");
        }

        public int Next(int minInclusive, int maxExclusive) => Math.Clamp(D100(), minInclusive, maxExclusive - 1);
    }

    /// <summary>Builds the contexts and suites the gate tests run on the test bench.</summary>
    internal static class BenchHarness
    {
        public static GateContext Context(IEnumerable<int> levels, int runs = 20, int seed = 1, int perLevel = 2) =>
            new(BenchRecipes.Catalog(levels, perLevel), SharedData.Gear, runs, seed, AttributeSweep.Default, enhancements: SharedData.Enhancements);

        public static CraftGateSuite Suite(ICraftScenario scenario, int recipesPerBand = 1, GateThresholds thresholds = null) =>
            new(scenario, (thresholds ?? GateThresholds.Default) with { RecipesPerBand = recipesPerBand });

        public static GateResult Evaluate(string gateKey, ICraftScenario scenario, GateContext context, int recipesPerBand = 1, GateThresholds thresholds = null) =>
            Suite(scenario, recipesPerBand, thresholds).Select(new[] { gateKey }).Single().Evaluate(context);

        /// <summary>The verdict cell of every row of a result's first table, which is its last column.</summary>
        public static IReadOnlyList<string> RowVerdicts(GateResult result) => result.Tables[0].Rows.Select(row => row[^1]).ToList();
    }
}
