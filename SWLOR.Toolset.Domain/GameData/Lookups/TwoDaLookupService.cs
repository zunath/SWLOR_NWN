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
            _twoDa.TablesReloaded += _cache.Clear;
            _tlk.CustomTlkReloaded += _cache.Clear;
        }

        /// <summary>
        /// Rows of <paramref name="tableName"/>, keyed by row index. <paramref name="labelColumn"/>
        /// supplies the fallback text. Blank and shared placeholder/sentinel labels are skipped;
        /// <paramref name="strRefColumn"/> is optional and supplies localized text when resolvable.
        /// Returns an empty list rather than throwing when the table or label column is missing, so
        /// a lookup that cannot be built degrades the field to a plain numeric box.
        /// </summary>
        public IReadOnlyList<TwoDaLookupRow> GetRows(string tableName, string labelColumn, string? strRefColumn = null) =>
            GetRows(tableName, labelColumn, strRefColumn, requiredColumns: null);

        /// <summary>Rows for a declared editor table, including its table-specific validity columns.</summary>
        public IReadOnlyList<TwoDaLookupRow> GetRows(TwoDaLookupTable table)
        {
            ArgumentNullException.ThrowIfNull(table);
            return GetRows(table.TableName, table.LabelColumn, table.StrRefColumn, table.RequiredColumns);
        }

        private IReadOnlyList<TwoDaLookupRow> GetRows(
            string tableName,
            string labelColumn,
            string? strRefColumn,
            IReadOnlyList<string>? requiredColumns)
        {
            var requirements = requiredColumns ?? Array.Empty<string>();
            var cacheKey = $"{tableName}|{labelColumn}|{strRefColumn}|{string.Join(',', requirements)}";
            return _cache.GetOrAdd(cacheKey, _ => Build(
                tableName,
                labelColumn,
                strRefColumn,
                requirements));
        }

        private IReadOnlyList<TwoDaLookupRow> Build(
            string tableName,
            string labelColumn,
            string? strRefColumn,
            IReadOnlyList<string> requiredColumns)
        {
            if (!_twoDa.TryGetTable(tableName, out var table) ||
                table == null ||
                !table.HasColumn(labelColumn) ||
                requiredColumns.Any(column => !table.HasColumn(column)))
            {
                return Array.Empty<TwoDaLookupRow>();
            }

            var hasStrRef = strRefColumn != null && table.HasColumn(strRefColumn);
            var results = new List<TwoDaLookupRow>(table.RowCount);

            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, labelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    requiredColumns.Any(column => string.IsNullOrWhiteSpace(table.GetString(row, column))))
                {
                    continue;
                }

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

                results.Add(new TwoDaLookupRow(row, label!, DisplayNameResolver.Resolve(_tlk, strRef, label!)));
            }

            return results;
        }
    }

    /// <summary>
    /// The 2DA tables wired to editor dropdowns through <see cref="TwoDaLookupService"/>, with the
    /// columns each one uses and any columns a real selectable row must populate. Column names are
    /// verified against the SWLOR_Haks/sw_2da corpus.
    /// </summary>
    public static class TwoDaLookupTables
    {
        /// <summary>gender.2da - NAME is a strref; CONSTANT ("GENDER_MALE") is the readable fallback.</summary>
        public static readonly TwoDaLookupTable Gender = new("gender", "CONSTANT", "NAME");

        /// <summary>phenotype.2da - Label ("Normal"), Name is the strref.</summary>
        public static readonly TwoDaLookupTable Phenotype = new("phenotype", "Label", "Name");

        /// <summary>soundset.2da - a real selectable row must name an audio RESREF.</summary>
        public static readonly TwoDaLookupTable SoundSet = new("soundset", "LABEL", "STRREF", ["RESREF"]);

        /// <summary>baseitems.2da - a real selectable row must declare an ItemClass.</summary>
        public static readonly TwoDaLookupTable BaseItem = new("baseitems", "label", "Name", ["ItemClass"]);

        /// <summary>
        /// placeables.2da model gallery - blank labels are valid when ModelName names a real model,
        /// but ModelName is required and both fields are screened for reserved sentinels.
        /// </summary>
        public static readonly TwoDaLookupTable PlaceableModel =
            new("placeables", "Label", "StrRef", ["ModelName"]);

        /// <summary>
        /// iprp_spells.2da - a Cast Spell subtype must identify a real spell, its levels, supported
        /// item usages, and an icon. Labeled disabled rows leave these cells blank.
        /// </summary>
        public static readonly TwoDaLookupTable ItemSpell = new(
            "iprp_spells",
            "Label",
            "Name",
            ["Name", "CasterLvl", "InnateLvl", "SpellIndex", "PotionUse", "WandUse", "GeneralUse", "Icon"]);

        /// <summary>
        /// Load screens. Read by label rather than by StrRef: every SWLOR row points at the same
        /// generic strref, so the label is the only thing that tells one screen from another.
        /// </summary>
        public static readonly TwoDaLookupTable LoadScreen = new("loadscreens", "Label", null);

        /// <summary>racialtypes.2da - a real race must declare the engine Constant it maps to.</summary>
        public static readonly TwoDaLookupTable Race = new("racialtypes", "Label", "Name", ["Constant"]);

        /// <summary>creaturespeed.2da - Label names each stored WalkRate row.</summary>
        public static readonly TwoDaLookupTable CreatureSpeed = new("creaturespeed", "Label", "Name");

        /// <summary>
        /// appearance.2da - selectable creatures need the model classification and model/race
        /// metadata used by the creature preview pipeline.
        /// </summary>
        public static readonly TwoDaLookupTable CreatureAppearance =
            new("appearance", "LABEL", "STRING_REF", ["NAME", "RACE", "MODELTYPE"]);

        /// <summary>waypoint.2da - a selectable marker needs a real model RESREF.</summary>
        public static readonly TwoDaLookupTable WaypointAppearance =
            new("waypoint", "LABEL", "STRREF", ["RESREF"]);

        /// <summary>portraits.2da - BaseResRef is both the identifier and resource stem.</summary>
        public static readonly TwoDaLookupTable Portrait = new("portraits", "BaseResRef", null);

        /// <summary>ambientsound.2da - Resource is the playable sound resref.</summary>
        public static readonly TwoDaLookupTable AmbientSound =
            new("ambientsound", "Resource", "Description");
    }

    /// <summary>A 2DA table plus the columns <see cref="TwoDaLookupService"/> should read from it.</summary>
    public sealed record TwoDaLookupTable(
        string TableName,
        string LabelColumn,
        string? StrRefColumn,
        IReadOnlyList<string>? RequiredColumns = null);
}
