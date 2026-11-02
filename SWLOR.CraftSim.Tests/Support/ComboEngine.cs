using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Support
{
    internal sealed record ComboState(MinigameMeters Meters, MinigameOpportunity NextOpportunity) : IMinigameState<ComboState>
    {
        public ComboState WithMeters(MinigameMeters meters) => this with { Meters = meters };
    }

    /// <summary>
    /// A minigame with no randomness at all and full state, where every recipe has a rotation of its own. Three actions each add ten
    /// percent of the progress the piece needs, and each adds ten percent of the quality too if it is the recipe's favored action and two
    /// percent if not, so ten actions make the item and the favored action alone makes it at full quality. A rotation that suits one
    /// recipe is poor for a recipe with another favored action: the best single rotation across recipes with different favorites scores
    /// well below the best rotation of each, while adaptive play (the previews show each action's quality) matches the best of each.
    /// </summary>
    internal sealed class ComboEngine : MinigameEngineBase<ComboState>
    {
        public const string Red = "red";
        public const string Green = "green";
        public const string Blue = "blue";

        public static readonly IReadOnlyList<string> All = new[] { Red, Green, Blue };

        private static readonly MinigameActionInfo[] Infos =
        {
            new(Red, "Red", "Progress, and quality if red is favored."),
            new(Green, "Green", "Progress, and quality if green is favored."),
            new(Blue, "Blue", "Progress, and quality if blue is favored.")
        };

        private readonly string _favored;
        private readonly IReadOnlyList<MinigameActionInfo> _actions;

        /// <param name="favored">The action that adds the full quality.</param>
        /// <param name="offered">The actions this recipe offers; all three when null.</param>
        public ComboEngine(IMinigameRandom random, string favored, IReadOnlyList<string> offered = null)
            : base(random, new ComboState(MinigameMeters.Start(100, 100, 100, 100), null))
        {
            _favored = favored;
            _actions = Infos.Where(info => offered == null || offered.Contains(info.Id)).ToArray();
        }

        public override CraftMinigameType Type => CraftMinigameType.Forge;

        public override IReadOnlyList<MinigameActionInfo> Actions => _actions;

        protected override MinigameActionRule<ComboState> ResolveAction(ComboState state, string actionId)
        {
            var info = _actions.FirstOrDefault(action => action.Id == actionId)
                       ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));
            var quality = actionId == _favored ? 10 : 2;
            return new MinigameActionRule<ComboState>(info, 0, 0, new[]
            {
                new MinigameOutcomeRule<ComboState>("Done", 100, s => s.Adjust(new MinigameMeterDelta(Progress: 10, Quality: quality)))
            });
        }
    }

    /// <summary>
    /// The scenario of <see cref="ComboEngine"/>: each recipe favors the action its type number selects, or all favor red with
    /// <see cref="SameFavoriteEverywhere"/>, so one rotation wins every craft.
    /// </summary>
    internal sealed class ComboScenario : ICraftScenario
    {
        public string Name => "combo";

        public string Title => "Combo Bench";

        public SkillType Skill => SkillType.Smithery;

        public CraftMinigameType Minigame => CraftMinigameType.Forge;

        public double SecondsPerAction => 6;

        /// <summary>Every recipe favors red: one fixed rotation is the best everywhere.</summary>
        public bool SameFavoriteEverywhere { get; init; }

        public AdaptivePolicyOptions AdaptiveOptions { get; init; } = new() { Depth = 2, ForecastSamples = 1, SampleBudget = 500 };

        public FixedRotationSolverOptions SolverOptions { get; init; } = BenchScenario.TinySolver;

        public EnhancementSupport Enhancements => EnhancementSupport.NoSlots;

        /// <summary>The action a recipe favors.</summary>
        public string FavoredBy(RecipeEntry recipe) => SameFavoriteEverywhere ? ComboEngine.Red : ComboEngine.All[(int)recipe.Type % ComboEngine.All.Count];

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request) => new ComboEngine(request.Random, FavoredBy(request.Recipe));
    }
}
