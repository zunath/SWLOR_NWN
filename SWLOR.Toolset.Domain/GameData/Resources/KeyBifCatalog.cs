using Nwn.Authoring.Resources;
using Nwn.Formats.Key;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Selects the NWN install's KEY archives in game precedence order and exposes their resources
    /// through the shared KEY/BIF readers. BIF bytes and metadata are loaded only when requested.
    /// </summary>
    public sealed class KeyBifCatalog
    {
        private readonly string _dataDirectory;
        private readonly IReadOnlyList<StockArchive> _archives;
        private readonly Dictionary<ResourceIdentity, (int KeyIndex, KeyResourceEntry Entry)> _index;
        // NWN:EE's xp3.bif exceeds 512 MiB. Streaming reads retain the independently bounded
        // metadata and requested payload, so the source-file limit covers that shipped archive.
        private static readonly ResourceLayerReadOptions ReadOptions = new()
        {
            MaximumBifFileBytes = 1024L * 1024 * 1024,
        };

        private KeyBifCatalog(
            string dataDirectory,
            IReadOnlyList<KeyFile> keyFiles,
            IReadOnlyList<StockArchive> archives)
        {
            _dataDirectory = dataDirectory;
            _archives = archives;
            _index = new Dictionary<ResourceIdentity, (int, KeyResourceEntry)>();

            for (var keyIndex = 0; keyIndex < keyFiles.Count; keyIndex++)
            {
                foreach (var entry in keyFiles[keyIndex].Resources)
                {
                    // The configured install list is ascending precedence; later keys replace earlier
                    // entries exactly as the game does.
                    _index[new ResourceIdentity(entry.ResRef, entry.RawTypeCode)] = (keyIndex, entry);
                }
            }
        }

        /// <summary>Total number of distinct resources indexed across loaded KEY archives.</summary>
        public int ResourceCount => _index.Count;

        /// <summary>Resource identities declared by the loaded KEY archives.</summary>
        public IEnumerable<ResourceIdentity> Resources => _index.Keys;

        /// <summary>
        /// Latest write time among the install's KEY/BIF archives. This coarse version invalidates
        /// previews when any base-game archive changes.
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

        /// <summary>NWN:EE archives in ascending precedence; later archives override earlier ones.</summary>
        private static readonly string[] KeyArchivesInPrecedenceOrder =
        [
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
        ];

        /// <summary>Loads the install's selected KEY archives from its data directory.</summary>
        public static KeyBifCatalog Load(string dataDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
            var fullDataDirectory = Path.GetFullPath(dataDirectory);
            var keyFiles = new List<KeyFile>();
            var archives = new List<StockArchive>();

            foreach (var name in KeyArchivesInPrecedenceOrder)
            {
                var keyPath = Path.Combine(fullDataDirectory, name);
                if (!File.Exists(keyPath))
                    continue;

                try
                {
                    var key = KeyReader.Read(ReadFileBounded(keyPath, ReadOptions.MaximumKeyFileBytes, "KEY index"));
                    keyFiles.Add(key);
                    archives.Add(CreateStockArchive(key, fullDataDirectory));
                }
                catch (Exception)
                {
                    // One unreadable optional archive must not hide resources from readable archives.
                }
            }

            if (keyFiles.Count == 0)
            {
                var keyPath = Path.Combine(fullDataDirectory, "nwn_base.key");
                var key = KeyReader.Read(ReadFileBounded(keyPath, ReadOptions.MaximumKeyFileBytes, "KEY index"));
                keyFiles.Add(key);
                archives.Add(CreateStockArchive(key, fullDataDirectory));
            }

            return new KeyBifCatalog(fullDataDirectory, keyFiles, archives);
        }

        /// <summary>Checks the KEY index without reading BIF contents.</summary>
        public bool Contains(ResourceIdentity identity) => _index.ContainsKey(identity);

        /// <summary>Reads one resource through the shared bounded stock-archive reader.</summary>
        public bool TryGetBytes(ResourceIdentity identity, out byte[] bytes, int maximumBytes = int.MaxValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maximumBytes);
            bytes = Array.Empty<byte>();
            if (!_index.TryGetValue(identity, out var indexed))
                return false;

            try
            {
                bytes = _archives[indexed.KeyIndex].ReadResource(indexed.Entry, maximumBytes);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        private static StockArchive CreateStockArchive(KeyFile key, string dataDirectory)
        {
            return StockArchive.FromStreams(key, bifFilename =>
            {
                var bifPath = ResolveBifPath(dataDirectory, bifFilename);
                if (bifPath is null)
                    throw new FileNotFoundException(
                        $"KEY BIF path '{bifFilename}' is outside the selected install.",
                        bifFilename);

                var stream = File.OpenRead(bifPath);
                if (stream.Length <= ReadOptions.MaximumBifFileBytes) return stream;
                var length = stream.Length;
                stream.Dispose();
                throw new FormatException($"BIF archive '{bifPath}' is {length} bytes; configured limit is {ReadOptions.MaximumBifFileBytes}.");
            }, ReadOptions.MaximumCachedBifBytes);
        }

        private static string? ResolveBifPath(string dataDirectory, string bifFilename)
        {
            var normalized = bifFilename
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            var installRoot = Path.GetDirectoryName(dataDirectory) ?? dataDirectory;
            var fullInstallRoot = Path.GetFullPath(installRoot);
            var fromInstallRoot = Path.GetFullPath(normalized, fullInstallRoot);
            var rootPrefix = fullInstallRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            if (!fromInstallRoot.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                return null;

            if (File.Exists(fromInstallRoot))
                return fromInstallRoot;

            var fromDataDirectory = Path.Combine(dataDirectory, Path.GetFileName(normalized));
            return File.Exists(fromDataDirectory) ? fromDataDirectory : null;
        }

        private static byte[] ReadFileBounded(string path, long maximumBytes, string description)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > maximumBytes || stream.Length > Array.MaxLength)
                throw new FormatException($"{description} '{path}' is {stream.Length} bytes; configured limit is {maximumBytes}.");
            var bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
            if (stream.ReadByte() != -1)
                throw new IOException($"{description} '{path}' grew while it was being read.");
            return bytes;
        }
    }
}
