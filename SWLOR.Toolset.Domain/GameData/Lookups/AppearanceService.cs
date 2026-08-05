using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// One row of appearance.2da (a creature "Appearance_Type"). Column layout confirmed against
    /// the SWLOR_Haks/sw_2da/appearance.2da corpus: LABEL, STRING_REF, NAME, RACE, ENVMAP,
    /// BLOODCOLR, MODELTYPE, ..., PORTRAIT, ...
    ///
    /// <see cref="Race"/> is dual-purpose depending on <see cref="ModelType"/>, exactly as the raw
    /// column is in the corpus: for MODELTYPE "S"/"F"/"W" (simple/full/warrior-style models) it
    /// holds the literal creature model ResRef (e.g. "c_badger"); for MODELTYPE "P" (segmented
    /// player-type models) it instead holds a single-letter phenotype/race code (e.g. "H" for
    /// Human), not a model ResRef. Callers that need a concrete model name for a "P" row must
    /// resolve it themselves (that requires combining phenotype/gender/part columns this table
    /// does not carry alone) - this service exposes the raw column verbatim rather than guessing.
    /// </summary>
    public sealed record AppearanceRow(
        int Id,
        string Label,
        string DisplayName,
        string? ModelType,
        string? Race,
        string? Portrait);

    /// <summary>
    /// Editor lookup over appearance.2da (the "Appearance_Type" dropdown). Results are built once
    /// on first use and cached for the lifetime of the service. Rows with an empty LABEL (roughly
    /// half of the corpus - unused/reserved appearance slots) are skipped, since they are never
    /// real content an editor would offer for selection.
    /// </summary>
    public sealed class AppearanceService
    {
        private const string TableName = "appearance";

        private readonly ReloadableLazy<IReadOnlyList<AppearanceRow>> _rows;
        private readonly ReloadableLazy<IReadOnlyDictionary<int, AppearanceRow>> _byId;

        public AppearanceService(TwoDaService twoDa, TlkService tlk)
        {
            if (twoDa is null) throw new ArgumentNullException(nameof(twoDa));
            if (tlk is null) throw new ArgumentNullException(nameof(tlk));

            _rows = new ReloadableLazy<IReadOnlyList<AppearanceRow>>(() => Build(twoDa, tlk));
            _byId = new ReloadableLazy<IReadOnlyDictionary<int, AppearanceRow>>(
                () => _rows.Value.ToDictionary(row => row.Id));
            twoDa.TablesReloaded += Invalidate;
            tlk.CustomTlkReloaded += Invalidate;
        }

        private void Invalidate()
        {
            _rows.Reset();
            _byId.Reset();
        }

        /// <summary>All non-reserved appearance.2da rows, in row (Appearance_Type) order.</summary>
        public IReadOnlyList<AppearanceRow> GetAll() => _rows.Value;

        /// <summary>Looks up a single row by its Appearance_Type id (the 2DA row number).</summary>
        public AppearanceRow Get(int id)
        {
            if (!_byId.Value.TryGetValue(id, out var row))
                throw new KeyNotFoundException($"Appearance row {id} was not found in appearance.2da.");

            return row;
        }

        private static IReadOnlyList<AppearanceRow> Build(TwoDaService twoDa, TlkService tlk)
        {
            var definition = TwoDaLookupTables.CreatureAppearance;
            var requiredColumns = definition.RequiredColumns!;
            if (!twoDa.TryGetTable(definition.TableName, out var table) ||
                table == null ||
                !table.HasColumn(definition.LabelColumn) ||
                requiredColumns.Any(column => !table.HasColumn(column)))
            {
                return Array.Empty<AppearanceRow>();
            }

            var results = new List<AppearanceRow>();

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, definition.LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    requiredColumns.Any(column =>
                        string.IsNullOrWhiteSpace(table.GetString(row, column))))
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

                var displayName = DisplayNameResolver.Resolve(tlk, strref, label!);

                results.Add(new AppearanceRow(
                    row,
                    label!,
                    displayName,
                    table.GetString(row, "MODELTYPE"),
                    table.GetString(row, "RACE"),
                    table.GetString(row, "PORTRAIT")));
            }

            return results;
        }
    }
}
