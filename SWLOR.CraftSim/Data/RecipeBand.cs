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

        /// <summary>Reads a range such as "46-50", named as written.</summary>
        public static RecipeBand Parse(string text)
        {
            var parts = text.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var first) && int.TryParse(parts[1], out var last) && first >= 1 && last >= first)
                return new RecipeBand(text, first, last);

            throw new ArgumentException($"'{text}' is not a recipe level range such as 46-50.");
        }
    }
}
