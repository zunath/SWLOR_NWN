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

        private readonly ReloadableLazy<IReadOnlyList<PlaceableAppearanceRow>> _rows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, PlaceableAppearanceRow>> _byId;

        public PlaceableAppearanceService(TwoDaService twoDa, TlkService tlk)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));
            if (tlk is null) throw new ArgumentNullException(nameof(tlk));

            _rows = new ReloadableLazy<IReadOnlyList<PlaceableAppearanceRow>>(() => Build(twoDa, tlk));
            _byId = new ReloadableLazy<IReadOnlyDictionary<int, PlaceableAppearanceRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
            twoDa.TablesReloaded += Invalidate;
            tlk.CustomTlkReloaded += Invalidate;
        }

        private void Invalidate()
        {
            _rows.Reset();
            _byId.Reset();
        }

        /// <summary>All non-reserved placeables.2da rows, in row (Appearance_Type) order.</summary>
        public IReadOnlyList<PlaceableAppearanceRow> GetAll() => _rows.Value;

        /// <summary>Looks up a single row by its placeable Appearance_Type id.</summary>
        public PlaceableAppearanceRow Get(int id)
        {
            if (!_byId.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Placeable appearance row {id} was not found in placeables.2da.");

            return row;
        }

        /// <summary>Tries to look up one row by its placeable Appearance_Type id.</summary>
        public bool TryGet(int id, out PlaceableAppearanceRow row)
        {
            return _byId.Value.TryGetValue(id, out row!);
        }

        private static IReadOnlyList<PlaceableAppearanceRow> Build(TwoDaService twoDa, TlkService tlk)
        {
            var definition = TwoDaLookupTables.PlaceableModel;
            var modelColumn = definition.RequiredColumns!.Single();
            var table = twoDa.GetTable(definition.TableName);
            if (!table.HasColumn(definition.LabelColumn) || !table.HasColumn(modelColumn))
                return Array.Empty<PlaceableAppearanceRow>();

            var results = new List<PlaceableAppearanceRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, definition.LabelColumn);
                var model = table.GetString(row, modelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    !TwoDaChoicePolicy.IsSelectableLabel(model))
                {
                    continue;
                }

                var strref = table.GetInt(row, definition.StrRefColumn!);
                var displayName = DisplayNameResolver.Resolve(tlk, strref, label!);

                results.Add(new PlaceableAppearanceRow(
                    row,
                    label!,
                    displayName,
                    model));
            }

            return results;
        }
    }
}
