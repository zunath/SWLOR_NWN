using SWLOR.CraftSim.Data;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>Everything the crafting baseline measured, and the inputs it measured with.</summary>
    public sealed class CraftingBaselineResults
    {
        public int Runs { get; init; }
        public int Seed { get; init; }
        public IReadOnlyList<string> Policies { get; init; }
        public IReadOnlyList<int> SensitivityOffsets { get; init; }
        public IReadOnlyList<GearLevel> GearLevels { get; init; }
        public IReadOnlyList<CraftCell> Cells { get; init; }
        public IReadOnlyList<CraftBandRow> Bands { get; init; }

        public static CraftingBaselineResults Run(RecipeCatalog recipes, CraftingGearCatalog gear, LegacyCraftBaselineOptions options)
        {
            var cells = LegacyCraftBaseline.Simulate(recipes, gear, options);
            return new CraftingBaselineResults
            {
                Runs = options.Runs,
                Seed = options.Seed,
                Policies = options.Policies.Select(p => p.Name).ToList(),
                SensitivityOffsets = options.SensitivityOffsets.Where(o => o != 0).Distinct().OrderBy(o => o).ToList(),
                GearLevels = options.Skills.SelectMany(gear.Levels).ToList(),
                Cells = cells,
                Bands = CraftBandSummary.Summarize(cells, recipes)
            };
        }
    }
}
