namespace SWLOR.CraftSim.Data
{
    /// <summary>A range of recipe levels the reports group together.</summary>
    public sealed record RecipeBand(string Name, int First, int Last)
    {
        /// <summary>1-10, 11-20, 21-30, 31-40 and 41-53 (the highest recipe level in the game; the last band takes any higher level too).</summary>
        public static readonly IReadOnlyList<RecipeBand> Default = new[]
        {
            new RecipeBand("1-10", 1, 10),
            new RecipeBand("11-20", 11, 20),
            new RecipeBand("21-30", 21, 30),
            new RecipeBand("31-40", 31, 40),
            new RecipeBand("41-53", 41, int.MaxValue)
        };

        public bool Contains(int level) => level >= First && level <= Last;
    }
}
