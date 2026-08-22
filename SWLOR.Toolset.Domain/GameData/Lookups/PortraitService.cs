using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// One row of portraits.2da. Column layout confirmed against the SWLOR_Haks/sw_2da/
    /// portraits.2da corpus: BaseResRef, Sex, Race, InanimateType, Plot, LowGore. There is no
    /// strref/TLK column on this table at all, so <see cref="DisplayName"/> is always
    /// <see cref="BaseResRef"/> - the field exists purely so callers of every lookup service in
    /// this namespace can rely on a uniform "Id/Label/DisplayName" shape.
    /// </summary>
    public sealed record PortraitRow(
        int Id,
        string BaseResRef,
        string DisplayName,
        int? Sex,
        int? Race,
        int? InanimateType,
        bool IsPlot = false);

    /// <summary>
    /// The five actual portrait TGA resrefs a <see cref="PortraitRow.BaseResRef"/> expands to,
    /// per NWN's "po_&lt;base&gt;&lt;size&gt;" naming convention: t(iny), s(mall), m(edium),
    /// l(arge), h(uge). <see cref="BaseResRef"/> values in the corpus already include their own
    /// trailing separator where one exists (e.g. "dw_f_01_"), so no extra separator is inserted
    /// here - each variant is simply "po_" + BaseResRef + the size letter.
    /// </summary>
    public readonly record struct PortraitTgaVariants(string Tiny, string Small, string Medium, string Large, string Huge);

    /// <summary>
    /// Editor lookup over portraits.2da (the portrait picker dropdown). Results are built once on
    /// first use and cached. Rows with an empty BaseResRef (unused/reserved slots) are skipped.
    /// </summary>
    public sealed class PortraitService
    {
        private const string TableName = "portraits";

        private readonly ReloadableLazy<IReadOnlyList<PortraitRow>> _rows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, PortraitRow>> _byId;

        public PortraitService(TwoDaService twoDa)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));

            _rows = new ReloadableLazy<IReadOnlyList<PortraitRow>>(() => Build(twoDa));
            _byId = new ReloadableLazy<IReadOnlyDictionary<int, PortraitRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
            twoDa.TablesReloaded += Invalidate;
        }

        private void Invalidate()
        {
            _rows.Reset();
            _byId.Reset();
        }

        /// <summary>All non-reserved portraits.2da rows, in row order.</summary>
        public IReadOnlyList<PortraitRow> GetAll() => _rows.Value;

        /// <summary>Looks up a single row by its portraits.2da row id.</summary>
        public PortraitRow Get(int id)
        {
            if (!_byId.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Portrait row {id} was not found in portraits.2da.");

            return row;
        }

        /// <summary>
        /// Expands a portraits.2da BaseResRef into the five size-variant portrait TGA resrefs, per
        /// NWN's "po_&lt;base&gt;&lt;size&gt;" convention. Pure string composition - does not check
        /// whether the resources actually exist; pair with a <see cref="Resources.ResourceIndex"/>
        /// lookup for that.
        /// </summary>
        public static PortraitTgaVariants GetTgaVariants(string baseResRef)
        {
            if (string.IsNullOrWhiteSpace(baseResRef))
                throw new ArgumentException("Base ResRef must not be empty.", nameof(baseResRef));

            return new PortraitTgaVariants(
                "po_" + baseResRef + "t",
                "po_" + baseResRef + "s",
                "po_" + baseResRef + "m",
                "po_" + baseResRef + "l",
                "po_" + baseResRef + "h");
        }

        private static IReadOnlyList<PortraitRow> Build(TwoDaService twoDa)
        {
            var definition = TwoDaLookupTables.Portrait;
            var table = twoDa.GetTable(definition.TableName);
            if (!table.HasColumn(definition.LabelColumn))
                return Array.Empty<PortraitRow>();

            var results = new List<PortraitRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var baseResRef = table.GetString(row, definition.LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(baseResRef))
                    continue;

                results.Add(new PortraitRow(
                    row,
                    baseResRef!,
                    baseResRef!,
                    TryGetInt(table, row, "Sex"),
                    TryGetInt(table, row, "Race"),
                    TryGetInt(table, row, "InanimateType"),
                    string.Equals(table.GetString(row, "Plot"), "1", StringComparison.Ordinal)));
            }

            return results;
        }

        /// <summary>
        /// Reads a cell as an integer, treating a non-numeric cell as "no value" rather than
        /// letting <see cref="FormatException"/> propagate and poison the caller's cached lookup.
        /// </summary>
        private static int? TryGetInt(TwoDaTable table, int row, string column)
        {
            try
            {
                return table.GetInt(row, column);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
