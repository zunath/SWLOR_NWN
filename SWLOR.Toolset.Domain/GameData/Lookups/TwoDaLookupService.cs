using System.Collections.Concurrent;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>One row of a simple label + strref 2DA lookup: the row index is the id stored in GFF.</summary>
    public sealed record TwoDaLookupRow(int Id, string Label, string DisplayName);

    /// <summary>
    /// A generic lookup over any 2DA that follows the common "identifier column + strref column"
    /// shape - gender, phenotype, soundset, baseitems, and friends. Those tables differ only in
    /// which columns to read, so they get one parameterized service rather than a near-identical
    /// class each (contrast <see cref="DoorTypeService"/> / <see cref="AppearanceService"/>, which
    /// earn their own types by exposing extra columns such as model resrefs).
    ///
    /// Display text resolves through <see cref="DisplayNameResolver"/>: the strref when the TLK has
    /// it, otherwise the row's own label. In this repo only the SWLOR custom TLK is loaded, so
    /// base-game strrefs normally fall back to the label - which is why every table wired here is
    /// one whose label column is human-readable ("shortsword", "Aasimar", "Normal").
    /// </summary>
    public sealed class TwoDaLookupService
    {
        private readonly TwoDaService _twoDa;
        private readonly TlkService _tlk;
        private readonly ConcurrentDictionary<string, IReadOnlyList<TwoDaLookupRow>> _cache = new(StringComparer.OrdinalIgnoreCase);

        public TwoDaLookupService(TwoDaService twoDa, TlkService tlk)
        {
            _twoDa = twoDa ?? throw new ArgumentNullException(nameof(twoDa));
            _tlk = tlk ?? throw new ArgumentNullException(nameof(tlk));
        }

        /// <summary>
        /// Rows of <paramref name="tableName"/>, keyed by row index. <paramref name="labelColumn"/>
        /// supplies the fallback text (and is required - a row with no label is treated as an
        /// unused/reserved slot and skipped, matching the other lookup services);
        /// <paramref name="strRefColumn"/> is optional and supplies localized text when resolvable.
        /// Returns an empty list rather than throwing when the table or label column is missing, so
        /// a lookup that cannot be built degrades the field to a plain numeric box.
        /// </summary>
        public IReadOnlyList<TwoDaLookupRow> GetRows(string tableName, string labelColumn, string? strRefColumn = null)
        {
            var cacheKey = $"{tableName}|{labelColumn}|{strRefColumn}";
            return _cache.GetOrAdd(cacheKey, _ => Build(tableName, labelColumn, strRefColumn));
        }

        private IReadOnlyList<TwoDaLookupRow> Build(string tableName, string labelColumn, string? strRefColumn)
        {
            if (!_twoDa.TryGetTable(tableName, out var table) || table == null || !table.HasColumn(labelColumn))
                return Array.Empty<TwoDaLookupRow>();

            var hasStrRef = strRefColumn != null && table.HasColumn(strRefColumn);
            var results = new List<TwoDaLookupRow>(table.RowCount);

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, labelColumn);
                if (string.IsNullOrWhiteSpace(label))
                    continue; // unused/reserved slot

                int? strRef = null;
                if (hasStrRef)
                {
                    try
                    {
                        strRef = table.GetInt(row, strRefColumn!);
                    }
                    catch (FormatException)
                    {
                        // A non-numeric cell in a strref column just means no localized text here.
                    }
                }

                results.Add(new TwoDaLookupRow(row, label, DisplayNameResolver.Resolve(_tlk, strRef, label)));
            }

            return results;
        }
    }

    /// <summary>
    /// The 2DA tables wired to editor dropdowns through <see cref="TwoDaLookupService"/>, with the
    /// columns each one uses. Column names verified against the SWLOR_Haks/sw_2da corpus.
    /// </summary>
    public static class TwoDaLookupTables
    {
        /// <summary>gender.2da - NAME is a strref; CONSTANT ("GENDER_MALE") is the readable fallback.</summary>
        public static readonly TwoDaLookupTable Gender = new("gender", "CONSTANT", "NAME");

        /// <summary>phenotype.2da - Label ("Normal"), Name is the strref.</summary>
        public static readonly TwoDaLookupTable Phenotype = new("phenotype", "Label", "Name");

        /// <summary>soundset.2da - LABEL ("Aasimar"), STRREF is the strref.</summary>
        public static readonly TwoDaLookupTable SoundSet = new("soundset", "LABEL", "STRREF");

        /// <summary>baseitems.2da - label ("shortsword"), Name is the strref.</summary>
        public static readonly TwoDaLookupTable BaseItem = new("baseitems", "label", "Name");

        /// <summary>
        /// Load screens. Read by label rather than by StrRef: every SWLOR row points at the same
        /// generic strref, so the label is the only thing that tells one screen from another.
        /// </summary>
        public static readonly TwoDaLookupTable LoadScreen = new("loadscreens", "Label", null);
    }

    /// <summary>A 2DA table plus the columns <see cref="TwoDaLookupService"/> should read from it.</summary>
    public sealed record TwoDaLookupTable(string TableName, string LabelColumn, string? StrRefColumn);
}
