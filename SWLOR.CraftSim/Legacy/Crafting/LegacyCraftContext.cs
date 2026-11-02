using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>One crafter attempting one recipe level: everything the old minigame fixes before the first action.</summary>
    public sealed class LegacyCraftContext
    {
        /// <param name="enhancements">The enhancements slotted into the craft; each adds its progress penalty to the progress the piece needs. Null for none.</param>
        public LegacyCraftContext(
            int recipeLevel, int rank, int control, int craftsmanship, int cpBonus, CraftEnhancementConfiguration enhancements = null)
        {
            RecipeLevel = recipeLevel;
            Rank = rank;
            Control = control;
            Craftsmanship = craftsmanship;
            CpBonus = cpBonus;
            Enhancements = enhancements ?? CraftEnhancementConfiguration.None;

            // RecipeLevelChart.cs:13-99, through the server's own Craft.GetRecipeLevelDetail.
            var level = Craft.GetRecipeLevelDetail(recipeLevel);
            MaxDurability = level.Durability;
            MaxProgress = LegacyEnhancementRules.MaxProgressWithEnhancements(
                LegacyCraftRules.MaxProgress(level.Progress, rank, recipeLevel), Enhancements.TotalProgressPenalty);
            MaxQuality = level.Quality;
            MaxCp = LegacyCraftRules.MaxCp(cpBonus, rank);
        }

        public int RecipeLevel { get; }
        public int Rank { get; }
        public int Control { get; }
        public int Craftsmanship { get; }
        public int CpBonus { get; }
        public int MaxCp { get; }
        public int MaxDurability { get; }

        /// <summary>The progress that completes the craft: the level chart's, adjusted for the rank difference, plus the enhancements' penalties.</summary>
        public int MaxProgress { get; }

        public int MaxQuality { get; }

        /// <summary>The enhancements slotted into the craft.</summary>
        public CraftEnhancementConfiguration Enhancements { get; }

        public int RankDifference => Rank - RecipeLevel;

        public bool IsUnlocked(LegacyAction action) => Rank >= LegacyActionSpec.Of(action).UnlockRank;

        public int ProgressOf(LegacyAction action) =>
            LegacyCraftRules.Progress(LegacyActionSpec.Of(action).Amount, Craftsmanship, Rank, RecipeLevel);

        public int QualityOf(LegacyAction action) =>
            LegacyCraftRules.Quality(LegacyActionSpec.Of(action).Amount, Control, Rank, RecipeLevel);

        public LegacyCraftState Start() => new()
        {
            Cp = MaxCp,
            Durability = MaxDurability,
            Outcome = LegacyCraftOutcome.InProgress
        };
    }
}
