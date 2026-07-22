using System.Collections.Concurrent;
using Radoub.Formats.Bif;
using Radoub.Formats.Key;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Loads nwn_base.key from an NWN install's "data" directory and exposes resource lookup by
    /// <see cref="ResourceIdentity"/>, resolving through the referenced BIF archives on demand.
    /// BIF metadata is read once per archive via <see cref="BifReader.ReadMetadataOnly(string)"/>
    /// (cheap - just the resource table); actual resource bytes are only extracted when
    /// <see cref="TryGetBytes"/> is called for a resource that lives in that archive.
    /// </summary>
    public sealed class KeyBifCatalog
    {
        private readonly string _dataDirectory;
        private readonly KeyFile _keyFile;
        private readonly Dictionary<ResourceIdentity, KeyResourceEntry> _index;
        private readonly ConcurrentDictionary<int, Lazy<BifFile?>> _bifCache = new();

        private KeyBifCatalog(string dataDirectory, KeyFile keyFile)
        {
            _dataDirectory = dataDirectory;
            _keyFile = keyFile;
            _index = new Dictionary<ResourceIdentity, KeyResourceEntry>();

            foreach (var entry in keyFile.ResourceEntries)
            {
                // nwn_base.key should not have duplicate resref+type pairs, but if it ever does,
                // last-wins matches the "later entry overrides" convention used elsewhere here.
                _index[new ResourceIdentity(entry.ResRef, entry.ResourceType)] = entry;
            }
        }

        /// <summary>
        /// Total number of resources indexed from nwn_base.key.
        /// </summary>
        public int ResourceCount => _index.Count;

        /// <summary>
        /// Load nwn_base.key from the given NWN install "data" directory.
        /// </summary>
        public static KeyBifCatalog Load(string dataDirectory)
        {
            var keyPath = Path.Combine(dataDirectory, "nwn_base.key");
            var keyFile = KeyReader.Read(keyPath);
            return new KeyBifCatalog(dataDirectory, keyFile);
        }

        /// <summary>
        /// Whether the catalog has an entry for the given resource, without extracting its bytes.
        /// </summary>
        public bool Contains(ResourceIdentity identity) => _index.ContainsKey(identity);

        public bool TryGetBytes(ResourceIdentity identity, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();

            if (!_index.TryGetValue(identity, out var entry))
                return false;

            var bifEntry = _keyFile.GetBifForResource(entry);
            if (bifEntry == null)
                return false;

            var bif = GetOrLoadBif(entry.BifIndex, bifEntry);
            if (bif == null)
                return false;

            var data = bif.ExtractVariableResource(entry.VariableTableIndex);
            if (data == null)
                return false;

            bytes = data;
            return true;
        }

        private BifFile? GetOrLoadBif(int bifIndex, KeyBifEntry bifEntry)
        {
            var lazyBif = _bifCache.GetOrAdd(
                bifIndex,
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

            var fromInstallRoot = Path.Combine(installRoot, normalized);
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
