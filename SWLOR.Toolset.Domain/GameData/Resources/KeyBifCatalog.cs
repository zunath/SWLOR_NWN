using System.Collections.Concurrent;
using SWLOR.NWN.Formats.Bif;
using SWLOR.NWN.Formats.Key;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Loads the NWN install's KEY archives from its "data" directory and exposes resource lookup by
    /// <see cref="ResourceIdentity"/>, resolving through the referenced BIF archives on demand.
    /// BIF metadata is read once per archive via <see cref="BifReader.ReadMetadataOnly(string)"/>
    /// (cheap - just the resource table); actual resource bytes are only extracted when
    /// <see cref="TryGetBytes"/> is called for a resource that lives in that archive.
    /// </summary>
    public sealed class KeyBifCatalog
    {
        private readonly string _dataDirectory;
        private readonly IReadOnlyList<KeyFile> _keyFiles;
        private readonly Dictionary<ResourceIdentity, (int KeyIndex, KeyResourceEntry Entry)> _index;
        private readonly ConcurrentDictionary<(int KeyIndex, int BifIndex), Lazy<BifFile?>> _bifCache = new();

        private KeyBifCatalog(string dataDirectory, IReadOnlyList<KeyFile> keyFiles)
        {
            _dataDirectory = dataDirectory;
            _keyFiles = keyFiles;
            _index = new Dictionary<ResourceIdentity, (int, KeyResourceEntry)>();

            for (var keyIndex = 0; keyIndex < keyFiles.Count; keyIndex++)
            {
                foreach (var entry in keyFiles[keyIndex].ResourceEntries)
                {
                    // Last wins, and the archives load in the game's own precedence order, so a retail
                    // or patch archive overrides the base one exactly as it does at runtime.
                    _index[new ResourceIdentity(entry.ResRef, entry.ResourceType)] = (keyIndex, entry);
                }
            }
        }

        /// <summary>
        /// Total number of resources indexed across every loaded KEY archive.
        /// </summary>
        public int ResourceCount => _index.Count;

        /// <summary>
        /// Resource identities declared by the loaded KEY archives. The index is immutable after loading,
        /// so callers may enumerate this collection concurrently with lazy BIF extraction.
        /// </summary>
        public IEnumerable<ResourceIdentity> Resources => _index.Keys;

        /// <summary>
        /// Latest write time among the install's KEY/BIF archives. This is intentionally a coarse
        /// content version: changing any base-game archive invalidates derived previews.
        /// </summary>
        public DateTime ContentVersionUtc
        {
            get
            {
                try
                {
                    return Directory.EnumerateFiles(_dataDirectory, "*", SearchOption.TopDirectoryOnly)
                        .Where(path =>
                            Path.GetExtension(path).Equals(".key", StringComparison.OrdinalIgnoreCase) ||
                            Path.GetExtension(path).Equals(".bif", StringComparison.OrdinalIgnoreCase))
                        .Select(File.GetLastWriteTimeUtc)
                        .DefaultIfEmpty(DateTime.MinValue)
                        .Max();
                }
                catch (Exception)
                {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// The KEY archives NWN:EE ships, in the order the game layers them - later overrides earlier.
        /// Any that is absent is skipped.
        /// </summary>
        /// <remarks>
        /// Official content is spread across several archives, not just the base one: a stock install
        /// carries nwn_base.key and nwn_retail.key. Loading only the base archive made everything the
        /// others hold look absent, so Standard palette entries were filtered out by ResolvableMembers
        /// and their models and textures failed to resolve.
        ///
        /// The list is deliberately every archive NWN:EE ships rather than the ones a particular
        /// install happens to have - the patch and localization archives (xp1patch, *_loc) hold real
        /// resources on the installs that carry them, and an install without one simply skips it via
        /// the File.Exists check below. A missing archive is invisible; a missing ENTRY resolves to
        /// an older override or to nothing at all, which is the failure this list exists to avoid.
        /// </remarks>
        private static readonly string[] KeyArchivesInPrecedenceOrder =
        {
            "nwn_base.key",
            "nwn_base_loc.key",
            "nwn_retail.key",
            "nwn_retail_loc.key",
            "xp1.key",
            "xp1_loc.key",
            "xp1patch.key",
            "xp1patch_loc.key",
            "xp2.key",
            "xp2_loc.key",
            "xp2patch.key",
            "xp2patch_loc.key",
            "xp3.key",
            "xp3_loc.key",
            "xp3patch.key",
            "xp3patch_loc.key"
        };

        /// <summary>
        /// Load the install's KEY archives from its "data" directory. At least one must be readable.
        /// </summary>
        public static KeyBifCatalog Load(string dataDirectory)
        {
            var loaded = new List<KeyFile>();

            foreach (var name in KeyArchivesInPrecedenceOrder)
            {
                var keyPath = Path.Combine(dataDirectory, name);
                if (!File.Exists(keyPath))
                    continue;

                try
                {
                    loaded.Add(KeyReader.Read(keyPath));
                }
                catch (Exception)
                {
                    // One unreadable archive must not cost the caller the ones that are fine.
                }
            }

            if (loaded.Count == 0)
            {
                // Preserve the original failure for a directory with no readable base archive.
                loaded.Add(KeyReader.Read(Path.Combine(dataDirectory, "nwn_base.key")));
            }

            return new KeyBifCatalog(dataDirectory, loaded);
        }

        /// <summary>
        /// Whether the catalog has an entry for the given resource, without extracting its bytes.
        /// </summary>
        public bool Contains(ResourceIdentity identity) => _index.ContainsKey(identity);

        public bool TryGetBytes(ResourceIdentity identity, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();

            if (!_index.TryGetValue(identity, out var indexed))
                return false;

            var (keyIndex, entry) = indexed;
            var bifEntry = _keyFiles[keyIndex].GetBifForResource(entry);
            if (bifEntry == null)
                return false;

            var bif = GetOrLoadBif(keyIndex, entry.BifIndex, bifEntry);
            if (bif == null)
                return false;

            var data = bif.ExtractVariableResource(entry.VariableTableIndex);
            if (data == null)
                return false;

            bytes = data;
            return true;
        }

        private BifFile? GetOrLoadBif(int keyIndex, int bifIndex, KeyBifEntry bifEntry)
        {
            // Keyed by archive as well as index: BIF indices are per-KEY, so two archives both have a
            // bif 0 and caching on the index alone would hand one archive's BIF to the other.
            var lazyBif = _bifCache.GetOrAdd(
                (keyIndex, bifIndex),
                _ => new Lazy<BifFile?>(
                    () => LoadBif(bifEntry),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            return lazyBif.Value;
        }

        private BifFile? LoadBif(KeyBifEntry bifEntry)
        {
            var bifPath = ResolveBifPath(bifEntry.Filename);
            return bifPath == null || !File.Exists(bifPath)
                ? null
                : BifReader.ReadMetadataOnly(bifPath);
        }

        private string? ResolveBifPath(string bifFilename)
        {
            // nwn_base.key stores BIF filenames as "data\xxx.bif", relative to the install root
            // (the parent of the "data" directory this catalog was loaded from).
            var normalized = bifFilename
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

            var installRoot = Path.GetDirectoryName(_dataDirectory) ?? _dataDirectory;

            var fullInstallRoot = Path.GetFullPath(installRoot);
            var fromInstallRoot = Path.GetFullPath(normalized, fullInstallRoot);
            var rootPrefix = fullInstallRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            if (!fromInstallRoot.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                return null;

            if (File.Exists(fromInstallRoot))
                return fromInstallRoot;

            // Fall back to just the bare filename directly under the data directory, in case the
            // stored path prefix doesn't match this install's actual layout.
            var justFilename = Path.GetFileName(normalized);
            var fromDataDirectory = Path.Combine(_dataDirectory, justFilename);
            return File.Exists(fromDataDirectory) ? fromDataDirectory : null;
        }
    }
}
