using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// One row of placeables.2da. Column layout confirmed against the SWLOR_Haks/sw_2da/
    /// placeables.2da corpus: Label, StrRef, ModelName, LightColor, ... Unlike most 2DAs in this
    /// namespace, the "Label" column here already holds human-readable text (e.g. "Armoire 1"),
    /// not an internal code - "StrRef" is a genuine (mostly base-game range) strref that overrides
    /// it when resolvable.
    /// </summary>
    public sealed record PlaceableAppearanceRow(
        int Id,
        string Label,
        string DisplayName,
        string? ModelName);

    /// <summary>
    /// Editor lookup over placeables.2da (the placeable "Appearance_Type" dropdown). Results are
    /// built once on first use and cached. Rows with an empty Label (the majority of the corpus -
    /// unused/reserved placeable slots) are skipped.
    /// </summary>
    public sealed class PlaceableAppearanceService
    {
        private const string TableName = "placeables";

        private readonly ReloadableLazy<LookupData> _data;

        public PlaceableAppearanceService(TwoDaService twoDa, TlkService tlk)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));
            if (tlk is null) throw new ArgumentNullException(nameof(tlk));

            _data = new ReloadableLazy<LookupData>(() => Build(twoDa, tlk));
            twoDa.TablesReloaded += Invalidate;
            tlk.CustomTlkReloaded += Invalidate;
        }

        private void Invalidate()
        {
            _data.Reset();
        }

        /// <summary>All non-reserved placeables.2da rows, in row (Appearance_Type) order.</summary>
        public IReadOnlyList<PlaceableAppearanceRow> GetAll() => _data.Value.SelectableRows;

        /// <summary>Looks up a single row by its placeable Appearance_Type id.</summary>
        public PlaceableAppearanceRow Get(int id)
        {
            if (!_data.Value.RenderRowsById.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Placeable appearance row {id} was not found in placeables.2da.");

            return row;
        }

        /// <summary>Tries to look up one row by its placeable Appearance_Type id.</summary>
        public bool TryGet(int id, out PlaceableAppearanceRow row)
        {
            return _data.Value.RenderRowsById.TryGetValue(id, out row!);
        }

        private static LookupData Build(TwoDaService twoDa, TlkService tlk)
        {
            var definition = TwoDaLookupTables.PlaceableModel;
            var modelColumn = definition.RequiredColumns!.Single();
            if (!twoDa.TryGetTable(definition.TableName, out var table) ||
                table == null ||
                !table.HasColumn(definition.LabelColumn) ||
                !table.HasColumn(modelColumn))
            {
                return LookupData.Empty;
            }

            var selectableRows = new List<PlaceableAppearanceRow>();
            var renderRowsById = new Dictionary<int, PlaceableAppearanceRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, definition.LabelColumn);
                var model = table.GetString(row, modelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(model))
                    continue;

                var selectableLabel = TwoDaChoicePolicy.IsSelectableLabel(label);
                var fallback = selectableLabel ? label! : model!;
                var strref = definition.StrRefColumn != null && table.HasColumn(definition.StrRefColumn)
                    ? table.GetInt(row, definition.StrRefColumn)
                    : null;
                var displayName = DisplayNameResolver.Resolve(tlk, strref, fallback);

                var result = new PlaceableAppearanceRow(
                    row,
                    fallback,
                    displayName,
                    model);
                renderRowsById.Add(row, result);
                if (selectableLabel)
                    selectableRows.Add(result);
            }

            return new LookupData(selectableRows, renderRowsById);
        }

        private sealed record LookupData(
            IReadOnlyList<PlaceableAppearanceRow> SelectableRows,
            IReadOnlyDictionary<int, PlaceableAppearanceRow> RenderRowsById)
        {
            public static LookupData Empty { get; } = new(
                Array.Empty<PlaceableAppearanceRow>(),
                new Dictionary<int, PlaceableAppearanceRow>());
        }
    }
}
