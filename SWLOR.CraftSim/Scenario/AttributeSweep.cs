namespace SWLOR.CraftSim.Scenario
{
    /// <summary>The attribute modifiers swept until the median attributes of active characters are measured.</summary>
    public sealed class AttributeSweep
    {
        public static readonly AttributeSweep Default = new(-1, 5);

        public AttributeSweep(int minimum, int maximum)
        {
            if (maximum < minimum)
                throw new ArgumentException("The sweep's maximum cannot be below its minimum.");

            Minimum = minimum;
            Maximum = maximum;
        }

        public int Minimum { get; }
        public int Maximum { get; }

        public IEnumerable<int> Modifiers => Enumerable.Range(Minimum, Maximum - Minimum + 1);

        /// <summary>An ability score whose modifier is <paramref name="modifier"/>: ten plus two per modifier point.</summary>
        public static int ScoreFor(int modifier) => 10 + 2 * modifier;
    }
}
