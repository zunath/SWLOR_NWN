using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.CraftSim.Data
{
    /// <summary>The spread of progress penalties among the enhancement items of one kind and level.</summary>
    public sealed record EnhancementSummary(int Count, int Minimum, double Median, int Maximum);

    /// <summary>
    /// The real enhancement items by kind and level, and the one the simulator uses to stand for them. A recipe's representative
    /// enhancement is the item of its kind at the highest level the recipe accepts (<see cref="LegacyEnhancementRules.IsAllowedFor"/>)
    /// whose progress penalty is the lower median of that level's items, ties broken by resref. Players use the best enhancement
    /// their recipe accepts, and the median keeps one unusually hard or easy item from setting the figure.
    /// </summary>
    public sealed class EnhancementCatalog
    {
        private readonly List<EnhancementItem> _items;
        private readonly List<string> _skipped;

        private EnhancementCatalog(IEnumerable<EnhancementItem> items, IEnumerable<string> skipped)
        {
            _items = items.OrderBy(i => i.Type).ThenBy(i => i.Level).ThenBy(i => i.ProgressPenalty).ThenBy(i => i.Resref, StringComparer.Ordinal).ToList();
            _skipped = skipped.ToList();
        }

        public IReadOnlyList<EnhancementItem> Items => _items;

        /// <summary>Items with a progress penalty that were left out, with the reason.</summary>
        public IReadOnlyList<string> Skipped => _skipped;

        public static EnhancementCatalog Build(IEnumerable<EnhancementItem> items, IEnumerable<string> skipped = null) =>
            new(items ?? throw new ArgumentNullException(nameof(items)), skipped ?? Array.Empty<string>());

        public static EnhancementCatalog Load(string repositoryRoot)
        {
            var read = EnhancementItemReader.ReadAll(RepositoryPaths.ModuleItems(repositoryRoot));
            return Build(read.Items, read.Skipped);
        }

        /// <summary>The kinds that have at least one item, in enum order.</summary>
        public IEnumerable<RecipeEnhancementType> Kinds => _items.Select(i => i.Type).Distinct();

        /// <summary>The distinct levels the items of a kind have, lowest first.</summary>
        public IReadOnlyList<int> Levels(RecipeEnhancementType type) =>
            _items.Where(i => i.Type == type).Select(i => i.Level).Distinct().OrderBy(l => l).ToList();

        public IReadOnlyList<EnhancementItem> ItemsOf(RecipeEnhancementType type, int level) =>
            _items.Where(i => i.Type == type && i.Level == level).ToList();

        /// <summary>The highest enhancement level of a kind that a recipe of this level accepts, or null when none is allowed.</summary>
        public int? TopLevelFor(RecipeEnhancementType type, int recipeLevel)
        {
            var allowed = Levels(type).Where(level => LegacyEnhancementRules.IsAllowedFor(level, recipeLevel)).ToList();
            return allowed.Count == 0 ? null : allowed[^1];
        }

        /// <summary>The item that stands for a kind and level: the lower median progress penalty, ties by resref. Null when there is none.</summary>
        public EnhancementItem Representative(RecipeEnhancementType type, int level)
        {
            var items = ItemsOf(type, level);
            return items.Count == 0 ? null : items[(items.Count - 1) / 2];
        }

        /// <summary>The representative enhancement for a recipe: the best level it accepts. Null when the recipe takes no enhancement of its kind.</summary>
        public EnhancementItem ForRecipe(RecipeEnhancementType type, int recipeLevel)
        {
            if (type == RecipeEnhancementType.None)
                return null;

            var level = TopLevelFor(type, recipeLevel);
            return level == null ? null : Representative(type, level.Value);
        }

        public EnhancementSummary Summarize(RecipeEnhancementType type, int level)
        {
            var penalties = ItemsOf(type, level).Select(i => i.ProgressPenalty).OrderBy(p => p).ToList();
            if (penalties.Count == 0)
                return new EnhancementSummary(0, 0, 0, 0);

            var middle = penalties.Count / 2;
            var median = penalties.Count % 2 == 1 ? penalties[middle] : (penalties[middle - 1] + penalties[middle]) / 2.0;
            return new EnhancementSummary(penalties.Count, penalties[0], median, penalties[^1]);
        }
    }
}
