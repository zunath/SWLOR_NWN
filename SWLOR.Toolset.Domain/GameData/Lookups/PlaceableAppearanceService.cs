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

        private readonly Lazy<IReadOnlyList<PlaceableAppearanceRow>> _rows;
        private readonly Lazy<IReadOnlyDictionary<int, PlaceableAppearanceRow>> _byId;

        public PlaceableAppearanceService(TwoDaService twoDa, TlkService tlk)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));
            if (tlk is null) throw new ArgumentNullException(nameof(tlk));

            _rows = new Lazy<IReadOnlyList<PlaceableAppearanceRow>>(() => Build(twoDa, tlk));
            _byId = new Lazy<IReadOnlyDictionary<int, PlaceableAppearanceRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
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

        private static IReadOnlyList<PlaceableAppearanceRow> Build(TwoDaService twoDa, TlkService tlk)
        {
            var table = twoDa.GetTable(TableName);
            var results = new List<PlaceableAppearanceRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, "Label");
                if (string.IsNullOrEmpty(label))
                    continue;

                var strref = table.GetInt(row, "StrRef");
                var displayName = DisplayNameResolver.Resolve(tlk, strref, label);

                results.Add(new PlaceableAppearanceRow(
                    row,
                    label,
                    displayName,
                    table.GetString(row, "ModelName")));
            }

            return results;
        }
    }
}
