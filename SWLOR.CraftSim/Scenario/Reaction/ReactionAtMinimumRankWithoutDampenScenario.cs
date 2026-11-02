namespace SWLOR.CraftSim.Scenario.Reaction
{
    /// <summary>The minimum-rank Reaction with Dampen removed, the comparator for <see cref="ReactionAtMinimumRankScenario"/>.</summary>
    public sealed class ReactionAtMinimumRankWithoutDampenScenario : ReactionAtMinimumRankScenario
    {
        public override string Name => "reaction-minrank-nodampen";

        public override string Title => "Reaction at the minimum rank without Dampen";

        protected override bool OffersDampen => false;
    }
}
