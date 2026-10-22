using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Support
{
    /// <summary>How a style changes the test engine: factors on what Press and Polish are worth in the window.</summary>
    internal sealed record BenchStyle(double PressFactor, double PolishFactor)
    {
        public static readonly BenchStyle Plain = new(1, 1);
    }

    /// <summary>
    /// The test-only scenario that plugs <see cref="BenchEngine"/> into the harness, the way a minigame plugs its own in. Its options
    /// build the cases the gates must tell apart: a dominant rotation, an unreachable quality, unbalanced styles, a slow window.
    /// </summary>
    internal sealed class BenchScenario : ICraftScenario
    {
        public static readonly CraftStyle Plain = new("Plain", BenchStyle.Plain);
        public static readonly CraftStyle PressStyle = new("Heavy Press", new BenchStyle(1.5, 1));
        public static readonly CraftStyle PolishStyle = new("Fine Polish", new BenchStyle(1, 1.5));
        public static readonly CraftStyle Overpowered = new("Overpowered", new BenchStyle(1.5, 1.5));

        public string Name { get; init; } = "bench";

        public string Title => "Test Bench";

        public SkillType Skill => SkillType.Smithery;

        public CraftMinigameType Minigame => CraftMinigameType.Forge;

        public double SecondsPerAction { get; init; } = 6;

        /// <summary>Adds the Perfect action, so a fixed rotation of it is as good as anything.</summary>
        public bool Dominant { get; init; }

        /// <summary>The quality meter's maximum as a percentage of the normal 100 (or of 60 and 140 when split by category).</summary>
        public int QualityDemand { get; init; } = 100;

        /// <summary>Breastplates need much progress and little quality; longswords the reverse.</summary>
        public bool SplitByCategory { get; init; }

        /// <summary>The integrity cost of Press; a high one makes crafts break.</summary>
        public int PressIntegrityCost { get; init; } = 6;

        public IReadOnlyList<CraftStylePair> StylePairs { get; init; } = Array.Empty<CraftStylePair>();

        public LengthTarget LengthTarget { get; init; } = LengthTarget.ItemCraft;

        public AdaptivePolicyOptions AdaptiveOptions { get; init; } = new() { Depth = 3, ForecastSamples = 1, SampleBudget = 2000 };

        public FixedRotationSolverOptions SolverOptions { get; init; } = new()
        {
            BeamWidth = 4, MaximumLength = 30, TrainingRuns = 24, SelectionRuns = 24, Finalists = 3, PolishPasses = 2,
            PolishStarts = 2, CycleLength = 3, ScreeningRuns = 8, AdaptiveSeedLines = 4
        };

        public IReadOnlyList<string> Limitations => new[] { "The test bench is not a real minigame." };

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var style = request.Style?.Configuration as BenchStyle ?? BenchStyle.Plain;
            int progress = 100, quality = 100;
            if (SplitByCategory)
            {
                (progress, quality) = request.Recipe.Category == RecipeCategoryType.Breastplate ? (140, 60) : (60, 140);
            }

            return new BenchEngine(
                request.Random,
                new BenchTuning
                {
                    MaxProgress = progress,
                    MaxQuality = quality * QualityDemand / 100,
                    MaxCp = 36 + request.Rank / 3 + request.Gear.CpBonus / 2,
                    PressInWindow = (int)((25 + request.Gear.Craftsmanship / 10) * style.PressFactor),
                    PolishInWindow = (int)((30 + request.Gear.Control / 10) * style.PolishFactor),
                    HasPerfect = Dominant,
                    PressIntegrityCost = PressIntegrityCost
                });
        }
    }

    internal static class BenchRecipes
    {
        /// <summary>Smithery recipes at the given levels, alternating breastplates and longswords, each with two component stacks.</summary>
        public static RecipeCatalog Catalog(IEnumerable<int> levels, int perLevel = 2)
        {
            var recipes = new List<RecipeEntry>();
            var number = 0;
            foreach (var level in levels)
            {
                for (var index = 0; index < perLevel; index++)
                {
                    number++;
                    recipes.Add(new RecipeEntry(
                        (RecipeType)(90000 + number),
                        SkillType.Smithery,
                        level,
                        $"bench_{number}",
                        number % 2 == 0 ? RecipeCategoryType.Longsword : RecipeCategoryType.Breastplate,
                        new Dictionary<string, int> { ["plate"] = 3, ["wire"] = 1 }));
                }
            }

            return RecipeCatalog.From(recipes);
        }

        /// <summary>Levels spread over the bands, with one in the top band's rank-50 range.</summary>
        public static readonly int[] SpreadLevels = { 5, 15, 25, 35, 45, 52 };
    }
}
