using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Every placeable appearance a builder can pick, for the model grid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately not <see cref="PlaceableAppearanceService"/>, which skips rows with an empty
    /// label because a dropdown cannot show a nameless option. That skip removes 15,761 perfectly
    /// good models from a picker whose entries are pictures - so this catalog keeps every row that
    /// resolves to a model and lets the tile do the naming.
    /// </para>
    /// <para>
    /// Rows with neither a label nor a model are dropped: there is nothing to draw and nothing to
    /// call it. A blueprint already pointing at one keeps its stored value; the editor marks it
    /// rather than blocking, which is what the 2,982 placeables on blank rows need.
    /// </para>
    /// </remarks>
    public sealed class PlaceableModelCatalog
    {
        private const string TableName = "placeables";

        private readonly Lazy<IReadOnlyList<PlaceableModelRow>> _rows;
        private readonly Lazy<IReadOnlyDictionary<int, PlaceableModelRow>> _byId;

        public PlaceableModelCatalog(TwoDaService twoDa, TlkService tlk)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            ArgumentNullException.ThrowIfNull(tlk);

            _rows = new Lazy<IReadOnlyList<PlaceableModelRow>>(() => Build(twoDa, tlk));
            _byId = new Lazy<IReadOnlyDictionary<int, PlaceableModelRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
        }

        /// <summary>
        /// True once the table has actually been read. The parse is shared by every editor, so
        /// the second placeable opened has nothing to wait for and should not be told it does.
        /// </summary>
        public bool IsBuilt => _rows.IsValueCreated;

        /// <summary>Every pickable row, in 2DA row order.</summary>
        public IReadOnlyList<PlaceableModelRow> GetAll() => _rows.Value;

        public bool TryGet(int id, out PlaceableModelRow row) => _byId.Value.TryGetValue(id, out row!);

        /// <summary>
        /// Rows whose label or model resref contains <paramref name="query"/>. An empty query
        /// returns everything, so callers page the result rather than binding it whole.
        /// </summary>
        public IEnumerable<PlaceableModelRow> Search(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAll();

            var trimmed = query.Trim();
            return GetAll().Where(row =>
                row.DisplayName.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                row.ModelName.Contains(trimmed, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<PlaceableModelRow> Build(TwoDaService twoDa, TlkService tlk)
        {
            var table = twoDa.GetTable(TableName);
            var rows = new List<PlaceableModelRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var model = table.GetString(row, "ModelName");
                if (string.IsNullOrEmpty(model))
                    continue;

                var label = table.GetString(row, "Label");
                var hasLabel = !string.IsNullOrEmpty(label);
                var displayName = hasLabel
                    ? DisplayNameResolver.Resolve(tlk, table.GetInt(row, "StrRef"), label!)
                    : model!;

                rows.Add(new PlaceableModelRow(row, model!, displayName, hasLabel));
            }

            return rows;
        }
    }
}
