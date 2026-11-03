using SWLOR.CraftSim.Data;

namespace SWLOR.CraftSim.Tests.Support
{
    /// <summary>The game data the simulator reads, loaded once for all tests.</summary>
    public static class SharedData
    {
        private static readonly Lazy<string> RootValue = new(() => RepositoryPaths.FindRoot());
        private static readonly Lazy<RecipeCatalog> RecipesValue = new(RecipeCatalog.Load);

        private static readonly Lazy<IReadOnlyList<GearItem>> GearItemsValue =
            new(() => GearItemReader.ReadAll(RepositoryPaths.ModuleItems(Root)));

        private static readonly Lazy<CraftingGearCatalog> GearValue =
            new(() => CraftingGearCatalog.Build(GearItems, Recipes));

        private static readonly Lazy<EnhancementCatalog> EnhancementsValue = new(() => EnhancementCatalog.Load(Root));

        public static string Root => RootValue.Value;
        public static RecipeCatalog Recipes => RecipesValue.Value;
        public static IReadOnlyList<GearItem> GearItems => GearItemsValue.Value;
        public static CraftingGearCatalog Gear => GearValue.Value;
        public static EnhancementCatalog Enhancements => EnhancementsValue.Value;
    }
}
