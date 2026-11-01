using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario
{
    /// <summary>One recipe-level band of a cohort: the recipes the gates play and how many active recipes the band has.</summary>
    /// <param name="Band">The level range.</param>
    /// <param name="Recipes">The recipes played, in level order: all of them, or an even sample.</param>
    /// <param name="ActiveRecipes">All the cohort's recipes in the band.</param>
    public sealed record CohortBand(RecipeBand Band, IReadOnlyList<RecipeEntry> Recipes, int ActiveRecipes);

    /// <summary>
    /// Which recipes a scenario's gates play and at which rank. The default is the plan's item-craft cohort: every active recipe
    /// of the skill from level 1, grouped into the default bands, with the crafter's rank equal to the recipe's level capped at 50.
    /// </summary>
    public sealed class ScenarioCohort
    {
        /// <summary>Highest rank any crafting skill reaches.</summary>
        public const int MaximumRank = 50;

        private readonly Func<RecipeEntry, bool> _includes;
        private readonly Func<RecipeEntry, int> _rankFor;

        public ScenarioCohort(Func<RecipeEntry, bool> includes, Func<RecipeEntry, int> rankFor = null, IReadOnlyList<RecipeBand> bands = null)
        {
            _includes = includes ?? throw new ArgumentNullException(nameof(includes));
            _rankFor = rankFor ?? AtLevelRank;
            Bands = bands ?? RecipeBand.Default;
        }

        /// <summary>The recipe-level bands the reports group by.</summary>
        public IReadOnlyList<RecipeBand> Bands { get; }

        /// <summary>All active recipes of one skill, at their own level.</summary>
        public static ScenarioCohort ForSkill(SkillType skill, Func<RecipeEntry, bool> also = null) =>
            new(recipe => recipe.Skill == skill && (also == null || also(recipe)));

        /// <summary>The crafter's rank for a recipe: its level, at most 50.</summary>
        public static int AtLevelRank(RecipeEntry recipe) => Math.Min(recipe.Level, MaximumRank);

        public int RankFor(RecipeEntry recipe) => _rankFor(recipe);

        /// <summary>The cohort's recipes from level 1, in level order.</summary>
        public IReadOnlyList<RecipeEntry> Recipes(RecipeCatalog catalog) =>
            catalog.Recipes
                .Where(recipe => recipe.Level >= 1 && _includes(recipe))
                .OrderBy(recipe => recipe.Level)
                .ThenBy(recipe => (int)recipe.Type)
                .ToList();

        /// <summary>
        /// The cohort grouped into <paramref name="bands"/> (the cohort's own by default), each with at most
        /// <paramref name="recipesPerBand"/> recipes chosen evenly across the band; bands with no recipe are left out.
        /// </summary>
        public IReadOnlyList<CohortBand> Select(RecipeCatalog catalog, int? recipesPerBand, IReadOnlyList<RecipeBand> bands = null)
        {
            var recipes = Recipes(catalog);
            var selected = new List<CohortBand>();
            foreach (var band in bands ?? Bands)
            {
                var inBand = recipes.Where(recipe => band.Contains(recipe.Level)).ToList();
                if (inBand.Count == 0)
                    continue;

                selected.Add(new CohortBand(band, Sample(inBand, recipesPerBand), inBand.Count));
            }

            return selected;
        }

        /// <summary>
        /// An even, repeatable sample: the recipes at evenly spaced positions of the ordered list, so the sample spans the band's levels
        /// and the recipe categories that sit together in the definitions. All of them when <paramref name="count"/> is null or large enough.
        /// </summary>
        public static IReadOnlyList<T> Sample<T>(IReadOnlyList<T> ordered, int? count)
        {
            if (count == null || count.Value >= ordered.Count)
                return ordered;

            if (count.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(count), "A sample needs at least one recipe.");

            var sample = new List<T>(count.Value);
            for (var index = 0; index < count.Value; index++)
                sample.Add(ordered[(int)((index + 0.5) * ordered.Count / count.Value)]);

            return sample;
        }
    }
}
