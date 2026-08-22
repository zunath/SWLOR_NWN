using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Resolves an itempropdef.2da CostTableResRef id to the highest CostValue that cost table
    /// actually offers - the real engine cap a numeric stat/requirement/appearance cell must never
    /// exceed. iprp_costtable.2da's own Name column names the target 2da for a given id (row 34 is
    /// "IPRP_DMG", which lowercases straight to iprp_dmg.2da - verified against
    /// SWLOR_Haks/sw_2da).
    /// </summary>
    /// <remarks>
    /// These tables are NOT all plain 0..N ladders. Some are sparse and some label their rows with
    /// something other than the row number: iprp_delay leaves rows 0-10 empty and labels row 11
    /// "110". Every populated row is therefore exposed through <see cref="OptionsFor"/> so the item
    /// editor can offer only legal values and show the row's gameplay meaning.
    /// </remarks>
    /// <remarks>
    /// Mirrors <see cref="SWLOR.Toolset.Domain.GameData.Lookups.BaseItemRowService"/>'s shape: a
    /// lazily-built dictionary over a 2da the caller may or may not have available.
    /// </remarks>
    public sealed class ItemCostTableRanges
    {
        /// <summary>Fallback cap when a cell's CostTableId is absent or cannot be resolved.</summary>
        public const int DefaultMax = ushort.MaxValue;

        private const string AmountColumn = "Amount";

        private readonly Lazy<IReadOnlyDictionary<int, int>> _maxByCostTableId;
        private readonly Lazy<IReadOnlyDictionary<int, IReadOnlyList<ItemCostTableOption>>> _optionsByCostTableId;

        public ItemCostTableRanges(TwoDaService twoDa)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            _maxByCostTableId = new Lazy<IReadOnlyDictionary<int, int>>(() => Build(twoDa));
            _optionsByCostTableId =
                new Lazy<IReadOnlyDictionary<int, IReadOnlyList<ItemCostTableOption>>>(() => BuildOptions(twoDa));
        }

        /// <summary>
        /// The rows a builder may actually choose from, with their gameplay-facing values; empty
        /// only when the cost table cannot be resolved.
        /// </summary>
        public IReadOnlyList<ItemCostTableOption> OptionsFor(int costTableId) =>
            costTableId >= 0 && _optionsByCostTableId.Value.TryGetValue(costTableId, out var options)
                ? options
                : Array.Empty<ItemCostTableOption>();

        private static IReadOnlyDictionary<int, IReadOnlyList<ItemCostTableOption>> BuildOptions(TwoDaService twoDa)
        {
            var result = new Dictionary<int, IReadOnlyList<ItemCostTableOption>>();
            var registryDefinition = TwoDaLookupTables.ItemCostTableRegistry;
            if (!twoDa.TryGetTable(registryDefinition.TableName, out var registry) ||
                registry == null ||
                !registry.HasColumn(registryDefinition.LabelColumn))
                return result;

            for (var row = 0; row < registry.RowCount; row++)
            {
                var name = registry.GetString(row, registryDefinition.LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(name))
                    continue;

                var targetDefinition = TwoDaLookupTables.ItemCostTable(name!);
                if (!twoDa.TryGetTable(targetDefinition.TableName, out var target) ||
                    target == null ||
                    target.RowCount == 0 ||
                    !target.HasColumn(targetDefinition.LabelColumn))
                    continue;

                var options = new List<ItemCostTableOption>();
                for (var value = 0; value < target.RowCount; value++)
                {
                    if (value > ushort.MaxValue)
                        break;

                    // A blank row is not a selectable CostValue. 2DA writes blanks as a run of
                    // asterisks, and the length of that run varies between tables, so the test is
                    // "nothing but asterisks" rather than a literal "****" - matching only the
                    // four-star spelling put rows of "*****" into the Utility dropdowns.
                    var label = target.GetString(value, targetDefinition.LabelColumn)?.Trim();
                    if (!TwoDaChoicePolicy.IsSelectableLabel(label))
                        continue;

                    // Cost-table labels are often engine-facing identifiers rather than the value a
                    // builder needs to choose. When a table publishes an Amount column, that is its
                    // semantic value: SWLOR resistance row 101, for example, stores CostValue 101
                    // but means -1 resistance. Fall back to the authored label for coded tables.
                    var amount = target.GetString(value, AmountColumn)?.Trim();
                    if (!string.IsNullOrWhiteSpace(amount) && !TwoDaChoicePolicy.IsSelectableLabel(amount))
                        continue;
                    var display = string.IsNullOrWhiteSpace(amount) ? label! : amount;

                    options.Add(new ItemCostTableOption(value, display));
                }

                var distinct = options.Select(option => option.Label)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count();

                // Nothing selectable or repeated display values cannot form an unambiguous list.
                if (options.Count == 0 ||
                    distinct != options.Count)
                {
                    continue;
                }

                result[row] = options;
            }

            return result;
        }

        /// <summary>The highest CostValue <paramref name="costTableId"/> offers, or null when it can't be resolved.</summary>
        public int? MaxFor(int costTableId) =>
            costTableId >= 0 && _maxByCostTableId.Value.TryGetValue(costTableId, out var max) ? max : null;

        private static IReadOnlyDictionary<int, int> Build(TwoDaService twoDa)
        {
            var result = new Dictionary<int, int>();
            var registryDefinition = TwoDaLookupTables.ItemCostTableRegistry;
            if (!twoDa.TryGetTable(registryDefinition.TableName, out var registry) ||
                registry == null ||
                !registry.HasColumn(registryDefinition.LabelColumn))
                return result;

            for (var row = 0; row < registry.RowCount; row++)
            {
                var name = registry.GetString(row, registryDefinition.LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(name))
                    continue;

                var targetDefinition = TwoDaLookupTables.ItemCostTable(name!);
                if (!twoDa.TryGetTable(targetDefinition.TableName, out var target) ||
                    target == null ||
                    target.RowCount == 0 ||
                    !target.HasColumn(targetDefinition.LabelColumn))
                    continue;

                var highestPopulated = -1;
                for (var targetRow = 0; targetRow < target.RowCount; targetRow++)
                {
                    if (TwoDaChoicePolicy.IsSelectableLabel(
                            target.GetString(targetRow, targetDefinition.LabelColumn)))
                        highestPopulated = targetRow;
                }

                if (highestPopulated >= 0)
                    result[row] = Math.Min(highestPopulated, ushort.MaxValue);
            }

            return result;
        }
    }
}
