namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>
    /// The fertilizer rules of the Player City Revamp, with no database or engine calls. A crop planted with Fertilizer of its tier
    /// or higher adds 1 to its harvest. Fertilizer is used up only by a planting that goes ahead, so every refusal leaves it in the
    /// planter's inventory.
    /// </summary>
    public static class FertilizerRules
    {
        public const string ResrefPrefix = "fertilizer_";

        public const int Tiers = FarmingRules.Tiers;

        /// <summary>The resref of Fertilizer I to V.</summary>
        public static string GetResref(int tier)
        {
            if (tier < 1 || tier > Tiers)
                throw new System.ArgumentOutOfRangeException(nameof(tier), tier, "A fertilizer tier is 1 to 5.");

            return ResrefPrefix + tier;
        }

        /// <summary>The tier of a fertilizer item, or 0 when the resref is not a fertilizer.</summary>
        public static int GetTier(string resref)
        {
            if (string.IsNullOrEmpty(resref) || !resref.StartsWith(ResrefPrefix, System.StringComparison.Ordinal))
                return 0;

            var tail = resref.Substring(ResrefPrefix.Length);
            return tail.Length == 1 && tail[0] >= '1' && tail[0] <= '0' + Tiers ? tail[0] - '0' : 0;
        }

        /// <summary>Whether Fertilizer of <paramref name="fertilizerTier"/> works on a crop of <paramref name="cropTier"/>: its tier or higher.</summary>
        public static bool Works(int fertilizerTier, int cropTier) => fertilizerTier >= cropTier;

        /// <summary>
        /// Why the fertilizer offered with a planting is refused, or <see cref="FarmingRefusal.None"/>. Nothing offered is never a
        /// refusal. Saplings refuse fertilizer in <see cref="WoodlotRules.GetFertilizerRefusal"/>, which the plant check asks first.
        /// </summary>
        /// <param name="fertilizerResref">The fertilizer offered, or empty for none.</param>
        /// <param name="carried">Whether the planter carries one of it.</param>
        /// <param name="cropTier">The tier of the crop being planted.</param>
        public static FarmingRefusal GetRefusal(string fertilizerResref, bool carried, int cropTier)
        {
            if (string.IsNullOrWhiteSpace(fertilizerResref))
                return FarmingRefusal.None;

            var tier = GetTier(fertilizerResref);
            if (tier == 0)
                return FarmingRefusal.NotFertilizer;
            if (!carried)
                return FarmingRefusal.NoFertilizer;
            if (!Works(tier, cropTier))
                return FarmingRefusal.FertilizerTooWeak;

            return FarmingRefusal.None;
        }

        /// <summary>The crops a fertilizer works on, as the planting window lists it.</summary>
        public static string DescribeReach(int tier) => tier <= 1 ? "tier 1 crops" : $"tier 1 to {tier} crops";

        /// <summary>The fertilizer as the planting window lists it: its name, how many the planter carries and what it works on.</summary>
        public static string DescribeOption(int tier, int count) =>
            $"Fertilizer {Numeral(tier)} x{count} ({DescribeReach(tier)})";

        private static string Numeral(int tier) => tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => "V"
        };
    }
}
