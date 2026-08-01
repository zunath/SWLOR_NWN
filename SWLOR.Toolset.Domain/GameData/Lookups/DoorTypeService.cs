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
    /// Editor lookup over doortypes.2da and genericdoors.2da. Results are built once on first use
    /// and cached. Placeholder labels and rows without the model/string metadata required by the
    /// builder-facing choices are skipped.
    /// </summary>
    public sealed class DoorTypeService
    {
        private const string TableName = "doortypes";
        private const string GenericTableName = "genericdoors";

        private readonly ReloadableLazy<IReadOnlyList<DoorTypeRow>> _rows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, DoorTypeRow>> _byId;
        private readonly ReloadableLazy<IReadOnlyList<GenericDoorRow>> _genericRows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, GenericDoorRow>> _genericById;

        public DoorTypeService(TwoDaService twoDa, TlkService tlk)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));
            if (tlk is null) throw new ArgumentNullException(nameof(tlk));

            _rows = new ReloadableLazy<IReadOnlyList<DoorTypeRow>>(() => Build(twoDa, tlk));
            _byId = new ReloadableLazy<IReadOnlyDictionary<int, DoorTypeRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
            _genericRows = new ReloadableLazy<IReadOnlyList<GenericDoorRow>>(() => BuildGeneric(twoDa, tlk));
            _genericById = new ReloadableLazy<IReadOnlyDictionary<int, GenericDoorRow>>(
                () => _genericRows.Value.ToDictionary(row => row.Id));
            twoDa.TablesReloaded += Invalidate;
            tlk.CustomTlkReloaded += Invalidate;
        }

        private void Invalidate()
        {
            _genericRows.Reset();
            _genericById.Reset();
            _rows.Reset();
            _byId.Reset();
        }

        /// <summary>All non-reserved doortypes.2da rows, in row order.</summary>
        public IReadOnlyList<DoorTypeRow> GetAll() => _rows.Value;

        /// <summary>All non-reserved genericdoors.2da rows.</summary>
        public IReadOnlyList<GenericDoorRow> GetGenericAll() => _genericRows.Value;

        /// <summary>Looks up a single specific model row by its Appearance id.</summary>
        public DoorTypeRow Get(int id)
        {
            if (!_byId.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Door type row {id} was not found in doortypes.2da.");

            return row;
        }

        public GenericDoorRow GetGeneric(int id)
        {
            if (!_genericById.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Generic door row {id} was not found in genericdoors.2da.");

            return row;
        }

        private static IReadOnlyList<DoorTypeRow> Build(TwoDaService twoDa, TlkService tlk)
        {
            var table = twoDa.GetTable(TableName);
            var results = new List<DoorTypeRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, "Label");
                var model = table.GetString(row, "Model");
                var stringRefGame = table.GetString(row, "StringRefGame");
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    string.IsNullOrWhiteSpace(model) ||
                    string.IsNullOrWhiteSpace(stringRefGame))
                {
                    continue;
                }

                var strref = table.GetInt(row, "StringRefGame");
                var displayName = DisplayNameResolver.Resolve(tlk, strref, label!);

                results.Add(new DoorTypeRow(
                    row,
                    label!,
                    displayName,
                    model));
            }

            return results;
        }

        private static IReadOnlyList<GenericDoorRow> BuildGeneric(TwoDaService twoDa, TlkService tlk)
        {
            var table = twoDa.GetTable(GenericTableName);
            var results = new List<GenericDoorRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, "Label");
                var model = table.GetString(row, "ModelName");
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    string.IsNullOrWhiteSpace(model))
                {
                    continue;
                }

                var nameStrRef = table.GetInt(row, "Name") ?? table.GetInt(row, "StrRef");
                results.Add(new GenericDoorRow(
                    row,
                    label!,
                    DisplayNameResolver.Resolve(tlk, nameStrRef, label!.Replace('_', ' ')),
                    model));
            }

            return results;
        }
    }
}
