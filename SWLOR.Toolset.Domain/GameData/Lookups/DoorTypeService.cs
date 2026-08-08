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
        string? Model)
    {
        /// <summary>
        /// Whether the door's model is visible in game. A false value identifies Aurora's
        /// toolset-only transition planes: the engine hides their model, but an editor must still
        /// draw their authored selection geometry so a builder can see and select them.
        /// </summary>
        public bool VisibleModel { get; init; } = true;
    }

    /// <summary>
    /// Editor lookup over doortypes.2da and genericdoors.2da. Results are built once on first use
    /// and cached. Placeholder labels and rows without the model/string metadata required by the
    /// builder-facing choices are skipped.
    /// </summary>
    public sealed class DoorTypeService
    {
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
            var definition = TwoDaLookupTables.DoorType;
            if (!twoDa.TryGetTable(definition.TableName, out var table) ||
                table == null ||
                !table.HasColumn(definition.LabelColumn) ||
                !table.HasColumn(definition.StrRefColumn!) ||
                definition.RequiredColumns!.Any(column => !table.HasColumn(column)))
            {
                return Array.Empty<DoorTypeRow>();
            }

            var results = new List<DoorTypeRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, definition.LabelColumn);
                var model = table.GetString(row, "Model");
                var stringRefGame = table.GetString(row, definition.StrRefColumn!);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    !TwoDaChoicePolicy.IsSelectableLabel(model) ||
                    !TwoDaChoicePolicy.IsSelectableLabel(stringRefGame))
                {
                    continue;
                }

                var visibleModel = TryGetInt(table, row, "VisibleModel");
                if (visibleModel is null)
                {
                    continue;
                }

                int? strref = null;
                try
                {
                    strref = table.GetInt(row, "StringRefGame");
                }
                catch (FormatException)
                {
                    // A non-numeric cell in the strref column just means no localized text here.
                }

                var displayName = DisplayNameResolver.Resolve(tlk, strref, label!);

                results.Add(new DoorTypeRow(
                    row,
                    label!,
                    displayName,
                    model)
                {
                    VisibleModel = visibleModel != 0
                });
            }

            return results;
        }

        private static IReadOnlyList<GenericDoorRow> BuildGeneric(TwoDaService twoDa, TlkService tlk)
        {
            var definition = TwoDaLookupTables.GenericDoor;
            if (!twoDa.TryGetTable(definition.TableName, out var table) ||
                table == null ||
                !table.HasColumn(definition.LabelColumn) ||
                definition.RequiredColumns!.Any(column => !table.HasColumn(column)))
            {
                return Array.Empty<GenericDoorRow>();
            }

            var results = new List<GenericDoorRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, definition.LabelColumn);
                var model = table.GetString(row, "ModelName");
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    !TwoDaChoicePolicy.IsSelectableLabel(model))
                {
                    continue;
                }

                var visibleModel = TryGetInt(table, row, "VisibleModel");
                if (visibleModel is null)
                {
                    continue;
                }

                var nameStrRef = TryGetInt(table, row, "Name") ?? TryGetInt(table, row, "StrRef");
                results.Add(new GenericDoorRow(
                    row,
                    label!,
                    DisplayNameResolver.Resolve(tlk, nameStrRef, label!.Replace('_', ' ')),
                    model)
                {
                    VisibleModel = visibleModel != 0
                });
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
