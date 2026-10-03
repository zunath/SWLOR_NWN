using System.Globalization;
using Nwn.Authoring.Doors;
using Nwn.Formats.TwoDa;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using FormatTwoDaTable = Nwn.Formats.TwoDa.TwoDaTable;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Editor lookup over doortypes.2da and genericdoors.2da. Results are built once on first use
    /// and cached; shared Authoring owns native row eligibility while this service owns TLK display.
    /// </summary>
    public sealed class DoorTypeService
    {
        private readonly ReloadableLazy<IReadOnlyList<DoorTypeRow>> _rows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, DoorTypeRow>> _byId;
        private readonly ReloadableLazy<IReadOnlyList<GenericDoorRow>> _genericRows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, GenericDoorRow>> _genericById;

        public DoorTypeService(TwoDaService twoDa, TlkService tlk)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            ArgumentNullException.ThrowIfNull(tlk);

            _rows = new ReloadableLazy<IReadOnlyList<DoorTypeRow>>(() => BuildSpecific(twoDa, tlk));
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

        /// <summary>All selectable doortypes.2da rows, in physical row order.</summary>
        public IReadOnlyList<DoorTypeRow> GetAll() => _rows.Value;

        /// <summary>All selectable genericdoors.2da rows, in physical row order.</summary>
        public IReadOnlyList<GenericDoorRow> GetGenericAll() => _genericRows.Value;

        /// <summary>Looks up a specific model row by its physical doortypes.2da row index.</summary>
        public DoorTypeRow Get(int id)
        {
            if (!_byId.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Door type row {id} was not found in doortypes.2da.");

            return row;
        }

        /// <summary>Looks up a generic model row by its physical genericdoors.2da row index.</summary>
        public GenericDoorRow GetGeneric(int id)
        {
            if (!_genericById.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Generic door row {id} was not found in genericdoors.2da.");

            return row;
        }

        private static IReadOnlyList<DoorTypeRow> BuildSpecific(TwoDaService twoDa, TlkService tlk)
        {
            var options = DoorAppearanceCatalogReader.Read(
                GetSnapshot(twoDa, TwoDaLookupTables.DoorType.TableName),
                null);
            return options
                .Where(option => option.Kind == DoorAppearanceKind.Specific)
                .Select(option => new DoorTypeRow(
                    checked((int)option.Id),
                    option.InternalLabel,
                    DisplayNameResolver.Resolve(tlk, option.StringRef, option.InternalLabel),
                    option.Model)
                {
                    VisibleModel = option.VisibleModel
                })
                .ToArray();
        }

        private static IReadOnlyList<GenericDoorRow> BuildGeneric(TwoDaService twoDa, TlkService tlk)
        {
            var options = DoorAppearanceCatalogReader.Read(
                null,
                GetSnapshot(twoDa, TwoDaLookupTables.GenericDoor.TableName));
            return options
                .Where(option => option.Kind == DoorAppearanceKind.Generic)
                .Select(option => new GenericDoorRow(
                    checked((int)option.Id),
                    option.InternalLabel,
                    DisplayNameResolver.Resolve(
                        tlk,
                        option.StringRef,
                        option.InternalLabel.Replace('_', ' ')),
                    option.Model)
                {
                    VisibleModel = option.VisibleModel
                })
                .ToArray();
        }

        private static FormatTwoDaTable? GetSnapshot(TwoDaService twoDa, string tableName)
        {
            if (!twoDa.TryGetTable(tableName, out var table) || table is null)
                return null;

            var snapshot = new FormatTwoDaTable(table.ColumnNames);
            for (var row = 0; row < table.RowCount; row++)
            {
                var values = table.ColumnNames.ToDictionary(
                    column => column,
                    column => table.GetString(row, column),
                    StringComparer.OrdinalIgnoreCase);
                var label = table.GetRowLabel(row) ?? row.ToString(CultureInfo.InvariantCulture);
                snapshot.AddRow(label, values);
            }

            return snapshot;
        }
    }
}
