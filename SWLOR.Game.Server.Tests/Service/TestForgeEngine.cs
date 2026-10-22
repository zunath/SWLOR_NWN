using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A small engine that exists only to prove the engine contract: costs, chance, state that changes later previews,
/// a telegraphed opportunity, an action that can finish the item, and one that can break it.
/// </summary>
internal sealed class TestForgeEngine : MinigameEngineBase
{
    public const string Strike = "strike";
    public const string Polish = "polish";
    public const string Mend = "mend";
    public const string Rush = "rush";
    public const string Locked = "locked";

    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = new[]
    {
        new MinigameActionInfo(Strike, "Strike", "Shapes the piece."),
        new MinigameActionInfo(Polish, "Polish", "Raises quality."),
        new MinigameActionInfo(Mend, "Mend", "Restores integrity."),
        new MinigameActionInfo(Rush, "Rush", "Finishes the piece or shatters it."),
        new MinigameActionInfo(Locked, "Locked", "Never available.")
    };

    private bool _boostAnnounced;

    public TestForgeEngine(IMinigameRandom random)
        : base(random, MinigameMeters.Start(maxProgress: 100, maxQuality: 100, maxIntegrity: 40, maxCp: 30))
    {
    }

    public override CraftMinigameType Type => CraftMinigameType.Forge;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public override MinigameOpportunity NextOpportunity => _boostAnnounced
        ? new MinigameOpportunity("stable", "Stable Containment", "The next Strike is 50% stronger.", 1)
        : null;

    protected override MinigameActionRule ResolveAction(string actionId)
    {
        var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        switch (actionId)
        {
            case Strike:
                var clean = _boostAnnounced ? 45 : 30;
                return new MinigameActionRule(info, cpCost: 2, integrityCost: 5, new[]
                {
                    new MinigameOutcomeRule("Clean strike", 75, new MinigameMeterDelta(Progress: clean, Quality: 5)),
                    new MinigameOutcomeRule("Glancing blow", 25, new MinigameMeterDelta(Progress: 10))
                });
            case Polish:
                return new MinigameActionRule(info, cpCost: 3, integrityCost: 2, new[]
                {
                    new MinigameOutcomeRule("Polished", 100, new MinigameMeterDelta(Quality: 20))
                });
            case Mend:
                return new MinigameActionRule(info, cpCost: 4, integrityCost: 0, new[]
                {
                    new MinigameOutcomeRule("Mended", 100, new MinigameMeterDelta(Integrity: 30))
                });
            case Rush:
                return new MinigameActionRule(info, cpCost: 1, integrityCost: 0, new[]
                {
                    new MinigameOutcomeRule("Breakthrough", 50, new MinigameMeterDelta(Progress: 1000)),
                    new MinigameOutcomeRule("Shatter", 50, new MinigameMeterDelta(Integrity: -1000))
                });
            default:
                return MinigameActionRule.Disabled(info, 0, 0, "Needs a hammer.");
        }
    }

    protected override void OnActionPerformed(MinigameActionResult result)
    {
        // Announced after every odd turn; it applies to the very next action.
        _boostAnnounced = Turn % 2 == 1;
    }
}
