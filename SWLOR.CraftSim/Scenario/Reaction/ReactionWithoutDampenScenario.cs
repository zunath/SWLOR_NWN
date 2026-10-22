using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Scenario.Reaction
{
    /// <summary>
    /// The Reaction with Dampen removed, for measuring what Dampen is worth: its gate report beside the Reaction's shows the same
    /// recipes, ranks and dice played without it. It is an analysis scenario, not a minigame that ships.
    /// </summary>
    public sealed class ReactionWithoutDampenScenario : ReactionScenario
    {
        public override string Name => "reaction-nodampen";

        public override string Title => "Reaction without Dampen";

        public override ICraftMinigameEngine CreateEngine(CraftEngineRequest request) => new DampenlessEngine(base.CreateEngine(request));
    }
}
