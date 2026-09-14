using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// One row of waypoint.2da: LABEL, RESREF, STRREF. RESREF is the marker model, and unlike
    /// placeables.2da there is no separate ModelName column - the resref is the model.
    /// </summary>
    public sealed record WaypointAppearanceRow(
        int Id,
        string Label,
        string DisplayName,
        string? ModelName);

    /// <summary>
    /// Editor lookup over waypoint.2da (the waypoint "Appearance" dropdown).
    /// </summary>
    /// <remarks>
    /// A waypoint does have artwork, contrary to how invisible it is in game: 76 rows of coloured
    /// flags, letters and symbols (treasure, mapnote, bullseye, snowflake, ...). Every .utw in this
    /// module carries an Appearance, so both the map marker and the palette preview can draw the
    /// real model rather than a generic shape.
    /// </remarks>
    public sealed class WaypointAppearanceService
    {
        private const string TableName = "waypoint";

        private readonly ReloadableLazy<IReadOnlyList<WaypointAppearanceRow>> _rows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, WaypointAppearanceRow>> _byId;

        public WaypointAppearanceService(TwoDaService twoDa, TlkService tlk)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));
            if (tlk is null) throw new ArgumentNullException(nameof(tlk));

            _rows = new ReloadableLazy<IReadOnlyList<WaypointAppearanceRow>>(() => Build(twoDa, tlk));
            _byId = new ReloadableLazy<IReadOnlyDictionary<int, WaypointAppearanceRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
            twoDa.TablesReloaded += Invalidate;
            tlk.CustomTlkReloaded += Invalidate;
        }

        private void Invalidate()
        {
            _rows.Reset();
            _byId.Reset();
        }

        /// <summary>All non-reserved waypoint.2da rows, in row (Appearance) order.</summary>
        public IReadOnlyList<WaypointAppearanceRow> GetAll() => _rows.Value;

        /// <summary>Tries to look up one row by its waypoint Appearance id.</summary>
        public bool TryGet(int id, out WaypointAppearanceRow row) => _byId.Value.TryGetValue(id, out row!);

        private static IReadOnlyList<WaypointAppearanceRow> Build(TwoDaService twoDa, TlkService tlk)
        {
            var definition = TwoDaLookupTables.WaypointAppearance;
            var modelColumn = definition.RequiredColumns!.Single();
            var table = twoDa.GetTable(definition.TableName);
            if (!table.HasColumn(definition.LabelColumn) || !table.HasColumn(modelColumn))
                return Array.Empty<WaypointAppearanceRow>();

            var results = new List<WaypointAppearanceRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, definition.LabelColumn);
                var model = table.GetString(row, modelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    !TwoDaChoicePolicy.IsSelectableLabel(model))
                {
                    continue;
                }

                int? strref = null;
                try
                {
                    strref = table.GetInt(row, definition.StrRefColumn!);
                }
                catch (FormatException)
                {
                    // A non-numeric cell in the strref column just means no localized text here.
                }

                results.Add(new WaypointAppearanceRow(
                    row,
                    label!,
                    DisplayNameResolver.Resolve(tlk, strref, label!),
                    model));
            }

            return results;
        }
    }
}
