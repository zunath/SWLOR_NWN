namespace SWLOR.CraftSim.Scenario
{
    /// <summary>The number of player actions a loop should take (CraftingRevampPlan.md, "Length targets").</summary>
    /// <param name="MinimumActions">The fewest actions per attempt on average.</param>
    /// <param name="MaximumActions">The most actions per attempt on average.</param>
    /// <param name="Label">What the target covers, for the report.</param>
    public sealed record LengthTarget(double MinimumActions, double MaximumActions, string Label)
    {
        /// <summary>At-level manual craft: 10-20 actions, about 1-2 minutes.</summary>
        public static readonly LengthTarget ItemCraft = new(10, 20, "item craft at level");

        /// <summary>
        /// A crafter who bought perk-line perks (owner decision, October 2026): the perks may shorten a craft, so the floor is 8 actions in every
        /// band and gear tier, while an unperked crafter keeps <see cref="ItemCraft"/>'s 10. The ceiling is the same.
        /// </summary>
        public static readonly LengthTarget PerkedItemCraft = new(8, 20, "item craft at level, perked build");

        public static readonly LengthTarget GatheringNode = new(3, 8, "gathering node");
        public static readonly LengthTarget FishingFight = new(3, 6, "fishing fight");
        public static readonly LengthTarget ResearchExperiment = new(5, 8, "research experiment");

        public bool Contains(double actions) => actions >= MinimumActions && actions <= MaximumActions;

        public override string ToString() => $"{MinimumActions:0.#}-{MaximumActions:0.#} actions ({Label})";
    }
}
