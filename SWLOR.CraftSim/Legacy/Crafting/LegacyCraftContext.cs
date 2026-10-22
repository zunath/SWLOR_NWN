using SWLOR.Game.Server.Service;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>One crafter attempting one recipe level: everything the old minigame fixes before the first action.</summary>
    public sealed class LegacyCraftContext
    {
        public LegacyCraftContext(int recipeLevel, int rank, int control, int craftsmanship, int cpBonus)
        {
            RecipeLevel = recipeLevel;
            Rank = rank;
            Control = control;
            Craftsmanship = craftsmanship;
            CpBonus = cpBonus;

            // RecipeLevelChart.cs:13-99, through the server's own Craft.GetRecipeLevelDetail.
            var level = Craft.GetRecipeLevelDetail(recipeLevel);
            MaxDurability = level.Durability;
            MaxProgress = LegacyCraftRules.MaxProgress(level.Progress, rank, recipeLevel);
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
        public int MaxProgress { get; }
        public int MaxQuality { get; }

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
