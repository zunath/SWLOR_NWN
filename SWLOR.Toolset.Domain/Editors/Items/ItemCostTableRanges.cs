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
    /// These tables are NOT all plain 0..N ladders, which is what this class used to assume. Some
    /// are sparse and some label their rows with something other than the row number: iprp_delay
    /// leaves rows 0-10 empty and labels row 11 "110". On those, the stored CostValue is a code
    /// rather than a quantity, and a number box is worse than useless - it lets a builder pick a row
    /// that does not exist and shows a number that means something else. Those tables are offered
    /// as <see cref="OptionsFor"/> instead.
    /// </remarks>
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

        private const string LabelColumn = "Label";

        /// <summary>
        /// Above this many rows a table is offered as a bounded number box even when it could be a
        /// list: iprp_dmg has 1,003 rows labelled "1".."1003", and a thousand-entry dropdown is a
        /// worse way to type a number than typing it.
        /// </summary>
        private const int MaximumListedOptions = 64;

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
        /// The rows a builder may actually choose from, when this table is a set of named choices
        /// rather than a range of numbers; empty when a number box is the right control.
        /// </summary>
        public IReadOnlyList<ItemCostTableOption> OptionsFor(int costTableId) =>
            costTableId >= 0 && _optionsByCostTableId.Value.TryGetValue(costTableId, out var options)
                ? options
                : Array.Empty<ItemCostTableOption>();

        private static IReadOnlyDictionary<int, IReadOnlyList<ItemCostTableOption>> BuildOptions(TwoDaService twoDa)
        {
            var result = new Dictionary<int, IReadOnlyList<ItemCostTableOption>>();
            if (!twoDa.TryGetTable(RegistryTable, out var registry) || registry == null)
                return result;

            for (var row = 0; row < registry.RowCount; row++)
            {
                var name = registry.GetString(row, NameColumn);
                if (string.IsNullOrWhiteSpace(name) || name == "****")
                    continue;

                if (!twoDa.TryGetTable(name, out var target) || target == null || target.RowCount == 0)
                    continue;

                var options = new List<ItemCostTableOption>();
                var everyLabelIsItsOwnRow = true;
                var previous = -1;
                var contiguous = true;

                for (var value = 0; value < target.RowCount; value++)
                {
                    // A blank row is not a selectable CostValue. 2DA writes blanks as a run of
                    // asterisks, and the length of that run varies between tables, so the test is
                    // "nothing but asterisks" rather than a literal "****" - matching only the
                    // four-star spelling put rows of "*****" into the Utility dropdowns.
                    var label = target.GetString(value, LabelColumn)?.Trim() ?? string.Empty;
                    if (label.Length == 0 || label.All(character => character == '*'))
                        continue;

                    // The label is shown as authored. Stripping a decorated ladder down to its number
                    // ("Resistance_001" -> "1") looked tidier and was wrong: that table indexes a
                    // resistance TYPE as well as an amount, so the numbers repeat and the prefix is
                    // the part that distinguishes them.
                    var display = label;

                    if (display != value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        everyLabelIsItsOwnRow = false;
                    if (previous >= 0 && value != previous + 1)
                        contiguous = false;

                    previous = value;
                    options.Add(new ItemCostTableOption(value, display));
                }

                // A dense ladder whose labels are just their row numbers IS a number - typing it is
                // better than scrolling for it. Anything else is a set of codes and must be listed.
                var distinct = options.Select(option => option.Label)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count();

                // Nothing selectable, a dense ladder better typed than scrolled, or labels that
                // repeat - a list of indistinguishable entries is worse than a number box.
                if (options.Count == 0 ||
                    distinct != options.Count ||
                    (everyLabelIsItsOwnRow && contiguous && options.Count > MaximumListedOptions))
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
