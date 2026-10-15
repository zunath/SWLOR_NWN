using Nwn.Authoring.Categories;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>
    /// SWLOR's category sidecar as the palette sees it. <see cref="CategoryService"/> keeps owning the
    /// file, its module write lock, external-change detection and seeding from <c>itp/*palcus.itp.json</c>.
    /// </summary>
    internal sealed class SwlorPaletteCategoryStore : IPaletteCategoryStore
    {
        private readonly CategoryService _categories;

        public SwlorPaletteCategoryStore(CategoryService categories)
        {
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        }

        public event Action? Changed
        {
            add => _categories.Changed += value;
            remove => _categories.Changed -= value;
        }

        public CategorySection? Section(ResourceType type) => _categories.Section(type);

        public PaletteCategorySaveResult CanSaveChanges() => Convert(_categories.CanSaveChanges());

        public PaletteCategorySaveResult SaveChanges() => Convert(_categories.SaveChanges());

        private static PaletteCategorySaveResult Convert(CategorySaveResult result) =>
            new(result.Saved, result.Problem);
    }
}
