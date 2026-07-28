using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Resolves an itempropdef.2da CostTableResRef id to the highest CostValue that cost table
    /// actually offers - the real engine cap a numeric stat/requirement/appearance cell must never
    /// exceed. iprp_costtable.2da's own Name column names the target 2da for a given id (row 34 is
    /// "IPRP_DMG", which lowercases straight to iprp_dmg.2da - verified against
    /// SWLOR_Haks/sw_2da); every one of these cost tables is a plain contiguous 0..N ladder, so the
    /// target table's row count minus one is its highest CostValue.
    /// </summary>
    /// <remarks>
    /// Mirrors <see cref="SWLOR.Toolset.Domain.GameData.Lookups.BaseItemRowService"/>'s shape: a
    /// lazily-built dictionary over a 2da the caller may or may not have available.
    /// </remarks>
    public sealed class ItemCostTableRanges
    {
        private const string RegistryTable = "iprp_costtable";
        private const string NameColumn = "Name";

        /// <summary>Fallback cap when a cell's CostTableId is absent or cannot be resolved.</summary>
        public const int DefaultMax = 255;

        private readonly Lazy<IReadOnlyDictionary<int, int>> _maxByCostTableId;

        public ItemCostTableRanges(TwoDaService twoDa)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            _maxByCostTableId = new Lazy<IReadOnlyDictionary<int, int>>(() => Build(twoDa));
        }

        /// <summary>The highest CostValue <paramref name="costTableId"/> offers, or null when it can't be resolved.</summary>
        public int? MaxFor(int costTableId) =>
            costTableId >= 0 && _maxByCostTableId.Value.TryGetValue(costTableId, out var max) ? max : null;

        private static IReadOnlyDictionary<int, int> Build(TwoDaService twoDa)
        {
            var result = new Dictionary<int, int>();
            if (!twoDa.TryGetTable(RegistryTable, out var registry) || registry == null)
                return result;

            for (var row = 0; row < registry.RowCount; row++)
            {
                var name = registry.GetString(row, NameColumn);
                if (string.IsNullOrWhiteSpace(name) || name == "****")
                    continue;

                if (!twoDa.TryGetTable(name, out var target) || target == null || target.RowCount == 0)
                    continue;

                result[row] = target.RowCount - 1;
            }

            return result;
        }
    }
}
