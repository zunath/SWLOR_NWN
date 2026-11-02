using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Everything the test engine knows: the shared meters, a telegraphed opportunity, and its own Thermal and hit count.</summary>
internal sealed record TestForgeState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    int Thermal,
    int Hits) : IMinigameState<TestForgeState>
{
    public TestForgeState WithMeters(MinigameMeters meters) => this with { Meters = meters };
}

/// <summary>
/// A small engine that exists only to prove the engine contract: costs, chance, an engine-specific meter (Thermal) that actions
/// raise and the end of the turn cools, a telegraphed opportunity rolled by the forecast, an action that can finish the item and
/// one that can break it.
/// </summary>
internal class TestForgeEngine : MinigameEngineBase<TestForgeState>
{
    public const string Heat = "heat";
    public const string Strike = "strike";
    public const string Polish = "polish";
    public const string Mend = "mend";
    public const string Rush = "rush";
    public const string Locked = "locked";

    public const string ThermalId = "thermal";
    public const int WorkingRangeFrom = 40;
    public const int WorkingRangeTo = 70;
    public const int Cooling = 5;
    public const int ScorchLine = 90;
    public const int OpportunityChancePercent = 40;

    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = new[]
    {
        new MinigameActionInfo(Heat, "Heat", "Raises Thermal."),
        new MinigameActionInfo(Strike, "Strike", "Shapes the piece; best inside the working range."),
        new MinigameActionInfo(Polish, "Polish", "Raises quality."),
        new MinigameActionInfo(Mend, "Mend", "Restores integrity."),
        new MinigameActionInfo(Rush, "Rush", "Finishes the piece or shatters it."),
        new MinigameActionInfo(Locked, "Locked", "Never available.")
    };

    public TestForgeEngine(IMinigameRandom random, int startingThermal = 50)
        : base(
            random,
            new TestForgeState(
                MinigameMeters.Start(maxProgress: 100, maxQuality: 100, maxIntegrity: 40, maxCp: 30),
                null,
                startingThermal,
                0))
    {
    }

    public override CraftMinigameType Type => CraftMinigameType.Forge;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public int Thermal => State.Thermal;

    public int Hits => State.Hits;

    protected override MinigameActionRule<TestForgeState> ResolveAction(TestForgeState state, string actionId)
    {
        var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        switch (actionId)
        {
            case Heat:
                return new MinigameActionRule<TestForgeState>(info, cpCost: 3, integrityCost: 0, new[]
                {
                    new MinigameOutcomeRule<TestForgeState>("Heated", 100,
                        s => s with { Thermal = Math.Min(100, s.Thermal + 25) })
                });
            case Strike:
                var inRange = state.Thermal >= WorkingRangeFrom && state.Thermal <= WorkingRangeTo;
                var clean = inRange ? 30 : 10;
                if (state.NextOpportunity != null)
                    clean += clean / 2;
                return new MinigameActionRule<TestForgeState>(info, cpCost: 2, integrityCost: 5, new[]
                {
                    new MinigameOutcomeRule<TestForgeState>("Clean strike", 75,
                        s => (s with { Hits = s.Hits + 1 }).Adjust(new MinigameMeterDelta(Progress: clean, Quality: 5))),
                    new MinigameOutcomeRule<TestForgeState>("Glancing blow", 25,
                        s => s.Adjust(new MinigameMeterDelta(Progress: 10)))
                });
            case Polish:
                return new MinigameActionRule<TestForgeState>(info, cpCost: 3, integrityCost: 2, new[]
                {
                    new MinigameOutcomeRule<TestForgeState>("Polished", 100, s => s.Adjust(new MinigameMeterDelta(Quality: 20)))
                });
            case Mend:
                return new MinigameActionRule<TestForgeState>(info, cpCost: 4, integrityCost: 0, new[]
                {
                    new MinigameOutcomeRule<TestForgeState>("Mended", 100, s => s.Adjust(new MinigameMeterDelta(Integrity: 30)))
                });
            case Rush:
                return new MinigameActionRule<TestForgeState>(info, cpCost: 1, integrityCost: 0, new[]
                {
                    new MinigameOutcomeRule<TestForgeState>("Breakthrough", 50, s => s.Adjust(new MinigameMeterDelta(Progress: 1000))),
                    new MinigameOutcomeRule<TestForgeState>("Shatter", 50, s => s.Adjust(new MinigameMeterDelta(Integrity: -1000)))
                });
            default:
                return MinigameActionRule<TestForgeState>.Disabled(info, 0, 0, "Needs a hammer.");
        }
    }

    protected override TestForgeState EndTurn(TestForgeState state)
    {
        // The piece cools, an announced opportunity has been used or has passed, and an overheated piece scorches.
        var cooled = state with { Thermal = Math.Max(0, state.Thermal - Cooling), NextOpportunity = null };
        return cooled.Thermal > ScorchLine ? cooled.Adjust(new MinigameMeterDelta(Integrity: -3)) : cooled;
    }

    protected override TestForgeState RollForecast(TestForgeState state, IMinigameRandom random) =>
        random.D100() <= OpportunityChancePercent
            ? state with
            {
                NextOpportunity = new MinigameOpportunity("stable", "Stable Containment", "The next Strike is 50% stronger.", 1)
            }
            : state;

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(TestForgeState state)
    {
        var shared = DescribeSharedMeters(state.Meters, progressLabel: "Shape", qualityLabel: "Finish");
        return shared.Append(new MinigameMeterView(
            ThermalId, "Thermal", state.Thermal, 0, 100,
            MinigameMeterMark.Range(WorkingRangeFrom, WorkingRangeTo, "Working range"))).ToList();
    }
}

/// <summary>An engine whose forecast moves a meter, which the contract forbids.</summary>
internal sealed class TestForgeEngineWithMeddlingForecast : TestForgeEngine
{
    public TestForgeEngineWithMeddlingForecast(IMinigameRandom random)
        : base(random)
    {
    }

    protected override TestForgeState RollForecast(TestForgeState state, IMinigameRandom random) =>
        state with { Thermal = state.Thermal + 1 };
}
