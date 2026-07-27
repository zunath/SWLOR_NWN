using System.Collections.Concurrent;
using SWLOR.NWN.Formats;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Toolset.Domain.GameData.TwoDa
{
    /// <summary>
    /// Loads and caches 2DA tables from a sw_2da directory. Table names are discovered eagerly
    /// (cheap directory listing) but table contents are parsed lazily on first request and cached
    /// for the lifetime of the service.
    ///
    /// Wraps SWLOR.NWN.Formats' <see cref="TwoDAReader"/>, which handles the sw_2da corpus's standard
    /// "2DA V2.0" text format directly (quoted fields, **** empty cells, optional UTF-8 BOM) - no
    /// custom parser was needed. One corpus file, "iprp_spells past.2da", is not a real 2DA file:
    /// it has no "2DA V2.0" signature line at all and looks like leftover scratch data pasted
    /// without a header, so no reader could recover a table from it. Rather than writing a parser
    /// for content that isn't structured 2DA data, <see cref="TryGetTable"/> reports it (and any
    /// other unparseable file) as unavailable instead of throwing, so callers that walk every known
    /// table name can tolerate it.
    /// </summary>
    public sealed class TwoDaService
    {
        private const string Extension = ".2da";

        private readonly Dictionary<string, string> _pathsByName;
        private readonly ConcurrentDictionary<string, Lazy<TwoDaTable>> _cache;

        public TwoDaService(string sw2DaDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(sw2DaDirectoryPath))
                throw new ArgumentException("2DA directory path must be provided.", nameof(sw2DaDirectoryPath));

            if (!Directory.Exists(sw2DaDirectoryPath))
                throw new DirectoryNotFoundException($"2DA directory not found: {sw2DaDirectoryPath}");

            DirectoryPath = sw2DaDirectoryPath;
            _pathsByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _cache = new ConcurrentDictionary<string, Lazy<TwoDaTable>>(StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in Directory.EnumerateFiles(sw2DaDirectoryPath, "*" + Extension))
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                _pathsByName[name] = filePath;
            }
        }

        public string DirectoryPath { get; }

        /// <summary>
        /// Smoke capability: all table names available in the directory (without extension),
        /// case as found on disk. Does not attempt to parse any file.
        /// </summary>
        public IReadOnlyCollection<string> GetTableNames()
        {
            return _pathsByName.Keys.ToArray();
        }

        /// <summary>
        /// Loads (or returns the cached) table with the given name (case-insensitive, no
        /// extension). Throws <see cref="KeyNotFoundException"/> if the name is unknown or the
        /// underlying file could not be parsed as a 2DA.
        /// </summary>
        public TwoDaTable GetTable(string name)
        {
            if (!TryGetTable(name, out var table))
                throw new KeyNotFoundException($"2DA table '{name}' was not found or could not be parsed.");

            return table!;
        }

        /// <summary>
        /// Attempts to load the table with the given name (case-insensitive, no extension).
        /// Returns false - rather than throwing - if the name is unknown or the underlying file
        /// could not be parsed as a 2DA.
        /// </summary>
        public bool TryGetTable(string name, out TwoDaTable? table)
        {
            if (!_pathsByName.TryGetValue(name, out var path))
            {
                table = null;
                return false;
            }

            var lazy = _cache.GetOrAdd(name, key => new Lazy<TwoDaTable>(
                () => new TwoDaTable(key, TwoDAReader.Read(path)),
                LazyThreadSafetyMode.ExecutionAndPublication));

            try
            {
                table = lazy.Value;
                return true;
            }
            catch (NwnFormatException)
            {
                // Malformed/non-2DA content (see class remarks) - tolerated rather than thrown.
                table = null;
                return false;
            }
        }
    }
}
