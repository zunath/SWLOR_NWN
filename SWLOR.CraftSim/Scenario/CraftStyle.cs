namespace SWLOR.CraftSim.Scenario
{
    /// <summary>
    /// A style or perk configuration a scenario can build its engine with, such as Hot Forging or Cold Working. The harness only
    /// names it and passes it back to the scenario in <see cref="CraftEngineRequest.Style"/>; what <see cref="Configuration"/>
    /// holds (stat adjustments, a perk set, an enum) is the scenario's own.
    /// </summary>
    public sealed class CraftStyle
    {
        public CraftStyle(string name, object configuration = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A style needs a name.", nameof(name));

            Name = name;
            Configuration = configuration;
        }

        public string Name { get; }

        /// <summary>Whatever the scenario needs to build an engine in this style; null for none.</summary>
        public object Configuration { get; }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Two styles that are alternatives to each other (only one can be active at a time), compared by the style-balance gate.
    /// </summary>
    /// <param name="First">One style.</param>
    /// <param name="Second">The other.</param>
    /// <param name="MinimumRank">The lowest skill rank at which a crafter can use either style (the plan puts a line's style at rank 20).</param>
    public sealed record CraftStylePair(CraftStyle First, CraftStyle Second, int MinimumRank = 20)
    {
        public string Name => $"{First.Name} vs {Second.Name}";
    }
}
