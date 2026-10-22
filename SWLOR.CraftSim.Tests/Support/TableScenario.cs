using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Support
{
    internal sealed record TableState(MinigameMeters Meters, MinigameOpportunity NextOpportunity) : IMinigameState<TableState>
    {
        public TableState WithMeters(MinigameMeters meters) => this with { Meters = meters };
    }

    /// <summary>An engine with one action that makes the item at a quality fixed in advance, so a gate's arithmetic can be checked exactly.</summary>
    internal sealed class TableEngine : MinigameEngineBase<TableState>
    {
        public const string Make = "make";

        private static readonly MinigameActionInfo[] ActionList = { new(Make, "Make", "Makes the item.") };

        private readonly int _qualityPoints;

        public TableEngine(IMinigameRandom random, int qualityPoints)
            : base(random, new TableState(MinigameMeters.Start(100, 100, 100, 10), null))
        {
            _qualityPoints = qualityPoints;
        }

        public override CraftMinigameType Type => CraftMinigameType.Forge;

        public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

        protected override MinigameActionRule<TableState> ResolveAction(TableState state, string actionId)
        {
            if (actionId != Make)
                throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

            var points = _qualityPoints;
            return new MinigameActionRule<TableState>(ActionList[0], 0, 0, new[]
            {
                new MinigameOutcomeRule<TableState>("Made", 100, s => s.Adjust(new MinigameMeterDelta(Progress: 100, Quality: points)))
            });
        }
    }

    /// <summary>A scenario whose quality for each recipe and style is a function the test supplies.</summary>
    internal sealed class TableScenario : ICraftScenario
    {
        public string Name => "table";
        public string Title => "Quality Table";
        public SkillType Skill => SkillType.Smithery;
        public CraftMinigameType Minigame => CraftMinigameType.Forge;
        public double SecondsPerAction => 6;

        /// <summary>The quality (0-100) of an item of this recipe made in this style (null for none).</summary>
        public Func<RecipeEntry, CraftStyle, int> Quality { get; init; } = (_, _) => 100;

        public IReadOnlyList<CraftStylePair> StylePairs { get; init; } = Array.Empty<CraftStylePair>();

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request) =>
            new TableEngine(request.Random, Quality(request.Recipe, request.Style));
    }
}
