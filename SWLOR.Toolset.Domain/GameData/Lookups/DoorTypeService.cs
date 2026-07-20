using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// One row of doortypes.2da. Column layout confirmed against the SWLOR_Haks/sw_2da/
    /// doortypes.2da corpus: Label, Model, TileSet, TemplateResRef, StringRefGame, BlockSight,
    /// VisibleModel, SoundAppType. "Label" here is an internal code (e.g. "Wall1Door"), not
    /// display text; "StringRefGame" is the strref that resolves to the real in-game door name.
    /// </summary>
    public sealed record DoorTypeRow(
        int Id,
        string Label,
        string DisplayName,
        string? Model);

    /// <summary>
    /// Editor lookup over doortypes.2da (the door "GenericType" dropdown). Results are built once
    /// on first use and cached. Rows with an empty Label (unused/reserved slots) are skipped.
    /// </summary>
    public sealed class DoorTypeService
    {
        private const string TableName = "doortypes";

        private readonly Lazy<IReadOnlyList<DoorTypeRow>> _rows;
        private readonly Lazy<IReadOnlyDictionary<int, DoorTypeRow>> _byId;

        public DoorTypeService(TwoDaService twoDa, TlkService tlk)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));
            if (tlk is null) throw new ArgumentNullException(nameof(tlk));

            _rows = new Lazy<IReadOnlyList<DoorTypeRow>>(() => Build(twoDa, tlk));
            _byId = new Lazy<IReadOnlyDictionary<int, DoorTypeRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
        }

        /// <summary>All non-reserved doortypes.2da rows, in row (GenericType) order.</summary>
        public IReadOnlyList<DoorTypeRow> GetAll() => _rows.Value;

        /// <summary>Looks up a single row by its door GenericType id.</summary>
        public DoorTypeRow Get(int id)
        {
            if (!_byId.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Door type row {id} was not found in doortypes.2da.");

            return row;
        }

        private static IReadOnlyList<DoorTypeRow> Build(TwoDaService twoDa, TlkService tlk)
        {
            var table = twoDa.GetTable(TableName);
            var results = new List<DoorTypeRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, "Label");
                if (string.IsNullOrEmpty(label))
                    continue;

                var strref = table.GetInt(row, "StringRefGame");
                var displayName = DisplayNameResolver.Resolve(tlk, strref, label);

                results.Add(new DoorTypeRow(
                    row,
                    label,
                    displayName,
                    table.GetString(row, "Model")));
            }

            return results;
        }
    }
}
