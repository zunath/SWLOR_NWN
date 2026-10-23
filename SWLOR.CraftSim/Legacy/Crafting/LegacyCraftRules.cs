namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>The old minigame's formulas, ported one for one from the deleted CraftViewModel (frozen at LegacySource.CraftViewModelCommit) with its float arithmetic and int truncation.</summary>
    public static class LegacyCraftRules
    {
        public const int CpPassiveRank = 25;
        public const int CpPassiveBonus = 31;
        public const int ProgressPassiveRank = 20;
        public const int ProgressPassiveBonus = 21;
        public const int QualityPassiveRank = 40;
        public const int QualityPassiveBonus = 115;

        /// <summary>The chance, in percent, that each component stack and enhancement is lost when a craft fails.</summary>
        public const int FailureLossPercent = 65;

        /// <summary>Highest skill rank any crafting skill reaches.</summary>
        public const int MaximumRank = 50;

        /// <summary>A recipe needs the crafter's rank to be at least its level minus this (Craft.cs:327-331).</summary>
        public const int RecipeUnlockOffset = 3;

        /// <summary>CraftViewModel.cs:446-454: gear CP plus 0.75 per rank, and a flat 31 from rank 25.</summary>
        public static int MaxCp(int cpBonus, int rank)
        {
            var maxCp = (int)(cpBonus + rank * 0.75f);
            if (rank >= CpPassiveRank)
                maxCp += CpPassiveBonus;

            return maxCp;
        }

        /// <summary>CraftViewModel.cs:460-478: both signs of the rank difference raise the progress needed, and the 2.0 cap tests a negative number.</summary>
        public static int MaxProgress(int levelProgress, int rank, int recipeLevel)
        {
            var levelDifference = rank - recipeLevel;
            var progressModifier = 0f;
            if (levelDifference < 0)
            {
                var adjustment = levelDifference * 0.25f;
                if (adjustment > 2.00f)
                    adjustment = 2.00f;
                progressModifier = -adjustment;
            }
            else if (levelDifference > 0)
            {
                var adjustment = levelDifference * 0.05f;
                if (adjustment > 0.25f)
                    adjustment = 0.25f;
                progressModifier = adjustment;
            }

            return (int)(levelProgress + levelProgress * progressModifier);
        }

        /// <summary>CraftViewModel.cs:1354-1367: progress of one synthesis. The rank-difference multiplier has no floor or cap.</summary>
        public static int Progress(int baseProgress, int craftsmanship, int rank, int recipeLevel)
        {
            var delta = rank - recipeLevel;
            var recipeDiff = 1 + 0.05f * delta;
            var steadyHandBonus = rank >= ProgressPassiveRank ? ProgressPassiveBonus : 0;
            return (int)((baseProgress + steadyHandBonus + craftsmanship * 0.65f) * recipeDiff);
        }

        /// <summary>CraftViewModel.cs:1369-1384: quality of one touch. Only a crafter below the recipe's level is penalised.</summary>
        public static int Quality(int baseQuality, int control, int rank, int recipeLevel)
        {
            var delta = rank - recipeLevel;
            var recipeDiff = delta < 0
                ? 1 + 0.05f * delta
                : 1;

            var muscleMemoryBonus = rank >= QualityPassiveRank ? QualityPassiveBonus : 0;
            return (int)((baseQuality + muscleMemoryBonus + control * 0.75f) * recipeDiff);
        }

        /// <summary>The lowest rank that may craft a recipe (Craft.cs:327-331).</summary>
        public static int RequiredRank(int recipeLevel) => Math.Max(recipeLevel - RecipeUnlockOffset, 0);

        /// <summary>Stacks consumed per success: n x (1 + 0.65 x (1 - p) / p), as failures lose each stack at 65% (CraftViewModel.cs:1601-1641).</summary>
        public static double StacksPerSuccess(int stacks, double successRate)
        {
            if (successRate <= 0)
                return double.PositiveInfinity;

            return stacks * (1 + FailureLossPercent / 100.0 * (1 - successRate) / successRate);
        }
    }
}
