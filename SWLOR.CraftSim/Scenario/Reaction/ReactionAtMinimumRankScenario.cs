namespace SWLOR.CraftSim.Scenario.Reaction
{
    /// <summary>
    /// The Reaction with every crafter at the lowest rank that can craft the recipe (three below its level), where the budget is tightest.
    /// An analysis scenario: it shows how Dampen and the other tools hold up when the crafter has the least power and CP.
    /// </summary>
    public class ReactionAtMinimumRankScenario : ReactionScenario
    {
        public override string Name => "reaction-minrank";

        public override string Title => "Reaction at the minimum rank";

        protected override int RankOffset => -3;
    }
}
