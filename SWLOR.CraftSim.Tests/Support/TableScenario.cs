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
        private readonly int _progressMax;

        public TableEngine(IMinigameRandom random, int qualityPoints, int progressMax = 100)
            : base(random, new TableState(MinigameMeters.Start(progressMax, 100, 100, 10), null))
        {
            _qualityPoints = qualityPoints;
            _progressMax = progressMax;
        }

        public override CraftMinigameType Type => CraftMinigameType.Forge;

        public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

        protected override MinigameActionRule<TableState> ResolveAction(TableState state, string actionId)
        {
            if (actionId != Make)
                throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

            var points = _qualityPoints;
            var progress = _progressMax;
            return new MinigameActionRule<TableState>(ActionList[0], 0, 0, new[]
            {
                new MinigameOutcomeRule<TableState>("Made", 100, s => s.Adjust(new MinigameMeterDelta(Progress: progress, Quality: points)))
            });
        }
    }

    /// <summary>A scenario whose quality for each recipe and style is a function the test supplies.</summary>
    internal sealed class TableScenario : ICraftScenario
    {
        /// <summary>The progress a unit of the old window's enhancement penalty adds to the table's piece: one for one, so any penalty shows.</summary>
        public const int ProgressPerPenaltyPoint = 1;

        public string Name => "table";
        public string Title => "Quality Table";
        public SkillType Skill => SkillType.Smithery;
        public CraftMinigameType Minigame => CraftMinigameType.Forge;
        public double SecondsPerAction => 6;

        /// <summary>The quality (0-100) of an item of this recipe made in this style (null for none).</summary>
        public Func<RecipeEntry, CraftStyle, int> Quality { get; init; } = (_, _) => 100;

        /// <summary>The quality (0-100) of an item of this recipe made with these enhancements; null uses <see cref="Quality"/> with no style.</summary>
        public Func<RecipeEntry, CraftEnhancementConfiguration, int> EnhancedQuality { get; init; }

        public EnhancementSupport Enhancements { get; init; } = EnhancementSupport.Modeled;

        public IReadOnlyList<CraftStylePair> StylePairs { get; init; } = Array.Empty<CraftStylePair>();

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var quality = request.Enhancements.IsNone || EnhancedQuality == null
                ? Quality(request.Recipe, request.Style)
                : EnhancedQuality(request.Recipe, request.Enhancements);
            return new TableEngine(request.Random, quality, 100 + request.Enhancements.TotalProgressPenalty * ProgressPerPenaltyPoint);
        }
    }
}
