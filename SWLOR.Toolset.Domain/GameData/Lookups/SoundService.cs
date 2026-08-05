using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// One row of ambientsound.2da. Column layout confirmed against the SWLOR_Haks/sw_2da/
    /// ambientsound.2da corpus: Description, Resource, PresetInstance0..7, DisplayName. Despite
    /// its name, "Description" is not literal text - it is the strref column (values are mostly
    /// base-game range, but SWLOR does append custom (>=16777216) strrefs for its own added
    /// sounds). "DisplayName" is a second, always-empty (in this corpus) override strref column.
    /// "Resource" is the actual playable sound resref.
    /// </summary>
    public sealed record SoundRow(
        int Id,
        string Resource,
        string DisplayName);

    /// <summary>
    /// Editor lookup over ambientsound.2da (the ambient sound dropdown). Results are built once on
    /// first use and cached. Rows with an empty Resource (the majority of the corpus - unused/
    /// reserved slots) are skipped, since there is nothing playable to offer for those rows.
    /// </summary>
    public sealed class SoundService
    {
        private const string TableName = "ambientsound";

        private readonly ReloadableLazy<IReadOnlyList<SoundRow>> _rows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, SoundRow>> _byId;

        public SoundService(TwoDaService twoDa, TlkService tlk)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));
            if (tlk is null) throw new ArgumentNullException(nameof(tlk));

            _rows = new ReloadableLazy<IReadOnlyList<SoundRow>>(() => Build(twoDa, tlk));
            _byId = new ReloadableLazy<IReadOnlyDictionary<int, SoundRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
            twoDa.TablesReloaded += Invalidate;
            tlk.CustomTlkReloaded += Invalidate;
        }

        private void Invalidate()
        {
            _rows.Reset();
            _byId.Reset();
        }

        /// <summary>All non-reserved ambientsound.2da rows, in row order.</summary>
        public IReadOnlyList<SoundRow> GetAll() => _rows.Value;

        /// <summary>Looks up a single row by its ambientsound.2da row id.</summary>
        public SoundRow Get(int id)
        {
            if (!_byId.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Ambient sound row {id} was not found in ambientsound.2da.");

            return row;
        }

        private static IReadOnlyList<SoundRow> Build(TwoDaService twoDa, TlkService tlk)
        {
            var definition = TwoDaLookupTables.AmbientSound;
            var table = twoDa.GetTable(definition.TableName);
            if (!table.HasColumn(definition.LabelColumn))
                return Array.Empty<SoundRow>();

            var results = new List<SoundRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var resource = table.GetString(row, definition.LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(resource))
                    continue;

                // Prefer the DisplayName override strref if a corpus entry ever populates it,
                // otherwise fall back to the (confusingly named) Description strref column, and
                // finally to the resource resref itself if neither strref resolves.
                var displayNameStrref = TryGetInt(table, row, "DisplayName");
                var descriptionStrref = TryGetInt(table, row, "Description");

                var displayName = DisplayNameResolver.Resolve(tlk, displayNameStrref,
                    DisplayNameResolver.Resolve(tlk, descriptionStrref, resource!));

                results.Add(new SoundRow(row, resource!, displayName));
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
