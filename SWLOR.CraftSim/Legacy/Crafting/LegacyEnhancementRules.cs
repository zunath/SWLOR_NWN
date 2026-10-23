namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>
    /// How enhancements work in the old craft window, ported one for one from CraftViewModel with its float arithmetic and int
    /// truncation: each enhancement adds its progress penalty to the progress the piece needs, and on a success each enhancement
    /// property transfers by an independent roll at a chance equal to the quality percentage.
    /// </summary>
    public static class LegacyEnhancementRules
    {
        /// <summary>The window has eight enhancement slots (CraftViewModel.cs:397-406).</summary>
        public const int MaximumSlots = 8;

        /// <summary>CraftViewModel.cs:397-406: a craft has its recipe's enhancement slots plus its blueprint's, shown up to eight.</summary>
        public static int Slots(int recipeSlots, int blueprintSlots) => Math.Min(recipeSlots + blueprintSlots, MaximumSlots);

        /// <summary>An enhancement is refused when its level is more than this many above the recipe's level (CraftViewModel.cs:601-666).</summary>
        public const int MaximumLevelAboveRecipe = 5;

        /// <summary>CraftViewModel.cs:601-666: an enhancement is accepted unless its level is more than five above the recipe's.</summary>
        public static bool IsAllowedFor(int enhancementLevel, int recipeLevel) =>
            enhancementLevel - recipeLevel <= MaximumLevelAboveRecipe;

        /// <summary>CraftViewModel.cs:696-755: the piece needs the sum of the enhancements' progress penalties on top of its maximum progress.</summary>
        public static int MaxProgressWithEnhancements(int maxProgress, int totalProgressPenalty) =>
            maxProgress + totalProgressPenalty;

        /// <summary>CraftViewModel.cs:1420: the chance, in percent, that each enhancement property transfers: the quality share, truncated.</summary>
        public static int TransferChancePercent(int quality, int maxQuality) =>
            (int)(((float)quality / (float)maxQuality) * 100);

        /// <summary>
        /// CraftViewModel.cs:1444-1468: each enhancement property rolls a D100 against the chance and transfers on a roll at or below it,
        /// so a property transfers with probability chance / 100. The expected number transferred by <paramref name="slots"/> enhancements.
        /// </summary>
        public static double ExpectedTransfers(int slots, int transferChancePercent) =>
            slots * (Math.Clamp(transferChancePercent, 0, 100) / 100.0);
    }
}
