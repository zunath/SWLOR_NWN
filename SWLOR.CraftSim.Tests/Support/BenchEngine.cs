using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Support
{
    /// <summary>Everything the test engine knows besides its configuration: the shared meters, a telegraphed opportunity and its Heat.</summary>
    internal sealed record BenchState(MinigameMeters Meters, MinigameOpportunity NextOpportunity, int Heat) : IMinigameState<BenchState>
    {
        public BenchState WithMeters(MinigameMeters meters) => this with { Meters = meters };
    }

    /// <summary>The immutable numbers one test engine is built from.</summary>
    internal sealed record BenchTuning
    {
        public int MaxProgress { get; init; } = 100;
        public int MaxQuality { get; init; } = 100;
        public int MaxIntegrity { get; init; } = 60;
        public int MaxCp { get; init; } = 40;
        public int PressInWindow { get; init; } = 25;
        public int PressOutside { get; init; } = 3;
        public int PolishInWindow { get; init; } = 30;
        public int PolishOutside { get; init; } = 2;

        /// <summary>Adds an action that adds progress and quality at a flat rate, whatever the Heat: a rotation of it beats everything.</summary>
        public bool HasPerfect { get; init; }

        public int PerfectProgress { get; init; } = 20;
        public int PerfectQuality { get; init; } = 25;
        public int PressIntegrityCost { get; init; } = 6;
    }

    /// <summary>
    /// A small engine that exists only to exercise the gates. Heat must be inside a working window for Press and Polish to work well;
    /// Stoke raises Heat by a chance-dependent amount and the piece cools every turn, so the right action depends on what the last
    /// Stoke did. A Clean Signal opportunity, rolled by the forecast, strengthens the next Polish. With <see cref="BenchTuning.HasPerfect"/>
    /// one action does everything at a flat rate, which is the dominant rotation the no-fixed-rotation gate must catch.
    /// </summary>
    internal sealed class BenchEngine : MinigameEngineBase<BenchState>
    {
        public const string Stoke = "stoke";
        public const string Press = "press";
        public const string Polish = "polish";
        public const string Mend = "mend";
        public const string Perfect = "perfect";

        public const string HeatId = "heat";
        public const int WindowFrom = 45;
        public const int WindowTo = 65;
        public const int Cooling = 10;
        public const int SignalChancePercent = 25;

        private static readonly MinigameActionInfo[] Basic =
        {
            new(Stoke, "Stoke", "Raises Heat by a varying amount."),
            new(Press, "Press", "Progress; strong while Heat is in the window."),
            new(Polish, "Polish", "Quality; strong while Heat is in the window."),
            new(Mend, "Mend", "Restores integrity.")
        };

        private static readonly MinigameActionInfo PerfectInfo = new(Perfect, "Perfect", "Progress and quality at a flat rate.");

        private readonly BenchTuning _tuning;
        private readonly IReadOnlyList<MinigameActionInfo> _actions;

        public BenchEngine(IMinigameRandom random, BenchTuning tuning)
            : base(
                random,
                new BenchState(
                    MinigameMeters.Start(tuning.MaxProgress, tuning.MaxQuality, tuning.MaxIntegrity, tuning.MaxCp),
                    null,
                    0))
        {
            _tuning = tuning;
            _actions = tuning.HasPerfect ? Basic.Append(PerfectInfo).ToArray() : Basic;
        }

        public override CraftMinigameType Type => CraftMinigameType.Forge;

        public override IReadOnlyList<MinigameActionInfo> Actions => _actions;

        public int Heat => State.Heat;

        protected override MinigameActionRule<BenchState> ResolveAction(BenchState state, string actionId)
        {
            var info = _actions.FirstOrDefault(action => action.Id == actionId)
                       ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));
            var inWindow = state.Heat >= WindowFrom && state.Heat <= WindowTo;

            switch (actionId)
            {
                case Stoke:
                    return new MinigameActionRule<BenchState>(info, cpCost: 2, integrityCost: 0, new[]
                    {
                        new MinigameOutcomeRule<BenchState>("Surge", 30, s => s with { Heat = Math.Min(100, s.Heat + 40) }),
                        new MinigameOutcomeRule<BenchState>("Warm", 40, s => s with { Heat = Math.Min(100, s.Heat + 25) }),
                        new MinigameOutcomeRule<BenchState>("Dim", 30, s => s with { Heat = Math.Min(100, s.Heat + 10) })
                    });
                case Press:
                    var progress = inWindow ? _tuning.PressInWindow : _tuning.PressOutside;
                    return new MinigameActionRule<BenchState>(info, cpCost: 0, integrityCost: _tuning.PressIntegrityCost, new[]
                    {
                        new MinigameOutcomeRule<BenchState>("Pressed", 100, s => s.Adjust(new MinigameMeterDelta(Progress: progress)))
                    });
                case Polish:
                    var quality = inWindow ? _tuning.PolishInWindow : _tuning.PolishOutside;
                    if (state.NextOpportunity != null)
                        quality += quality / 2;

                    return new MinigameActionRule<BenchState>(info, cpCost: 3, integrityCost: 3, new[]
                    {
                        new MinigameOutcomeRule<BenchState>("Polished", 100, s => s.Adjust(new MinigameMeterDelta(Quality: quality)))
                    });
                case Mend:
                    return new MinigameActionRule<BenchState>(info, cpCost: 5, integrityCost: 0, new[]
                    {
                        new MinigameOutcomeRule<BenchState>("Mended", 100, s => s.Adjust(new MinigameMeterDelta(Integrity: 25)))
                    });
                case Perfect:
                    return new MinigameActionRule<BenchState>(info, cpCost: 0, integrityCost: 1, new[]
                    {
                        new MinigameOutcomeRule<BenchState>("Perfect", 100,
                            s => s.Adjust(new MinigameMeterDelta(Progress: _tuning.PerfectProgress, Quality: _tuning.PerfectQuality)))
                    });
                default:
                    throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));
            }
        }

        protected override BenchState EndTurn(BenchState state) =>
            state with { Heat = Math.Max(0, state.Heat - Cooling), NextOpportunity = null };

        protected override BenchState RollForecast(BenchState state, IMinigameRandom random) =>
            random.D100() <= SignalChancePercent
                ? state with { NextOpportunity = new MinigameOpportunity("signal", "Clean Signal", "The next Polish is 50% stronger.", 1) }
                : state;

        protected override IReadOnlyList<MinigameMeterView> DescribeMeters(BenchState state) =>
            DescribeSharedMeters(state.Meters, progressLabel: "Shape", qualityLabel: "Finish")
                .Append(new MinigameMeterView(HeatId, "Heat", state.Heat, 0, 100, MinigameMeterMark.Range(WindowFrom, WindowTo, "Working window")))
                .ToList();
    }
}
