using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Data
{
    /// <summary>Reads the active recipes from the server's recipe definitions.</summary>
    public sealed class RecipeCatalog
    {
        private readonly List<RecipeEntry> _recipes = new();
        private readonly List<string> _loadErrors = new();

        private RecipeCatalog()
        {
        }

        public IReadOnlyList<RecipeEntry> Recipes => _recipes;
        public IReadOnlyList<string> LoadErrors => _loadErrors;

        public static RecipeCatalog Load()
        {
            var catalog = new RecipeCatalog();
            catalog.LoadAll();
            return catalog;
        }

        /// <summary>Builds a catalog from recipes created elsewhere, such as a test's hand-written ones.</summary>
        public static RecipeCatalog From(IEnumerable<RecipeEntry> recipes)
        {
            var catalog = new RecipeCatalog();
            catalog._recipes.AddRange(recipes);
            return catalog;
        }

        /// <summary>Active recipes of one skill, in level order.</summary>
        public IEnumerable<RecipeEntry> ForSkill(SkillType skill) =>
            _recipes.Where(r => r.Skill == skill).OrderBy(r => r.Level);

        /// <summary>Active recipes of one skill that a crafter can make without learning them first, in level order.</summary>
        public IEnumerable<RecipeEntry> ForNewCrafter(SkillType skill) =>
            ForSkill(skill).Where(r => !r.RequiresLearning);

        private void LoadAll()
        {
            var seen = new HashSet<RecipeType>();
            foreach (var type in typeof(IRecipeListDefinition).Assembly.GetTypes()
                         .Where(t => typeof(IRecipeListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                         .OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                Dictionary<RecipeType, RecipeDetail> recipes;
                try
                {
                    recipes = ((IRecipeListDefinition)Activator.CreateInstance(type)).BuildRecipes();
                }
                catch (Exception ex)
                {
                    _loadErrors.Add($"{type.Name}: {ex.GetType().Name}");
                    continue;
                }

                foreach (var (recipeType, detail) in recipes.OrderBy(r => (int)r.Key))
                {
                    // The server keeps the first definition of a duplicated recipe.
                    if (!seen.Add(recipeType))
                        continue;

                    if (!detail.IsActive)
                        continue;

                    _recipes.Add(new RecipeEntry(recipeType, detail));
                }
            }
        }
    }
}
