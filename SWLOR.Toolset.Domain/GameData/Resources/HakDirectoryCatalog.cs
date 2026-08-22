using SWLOR.NWN.Formats.Common;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Indexes one loose-file hak source folder (e.g. SWLOR_Haks\sw_t_dungeon) by scanning its
    /// directory listing. <see cref="Scan"/> only touches file names/extensions - no file content
    /// is read until <see cref="TryGetBytes"/> is called for a specific resource.
    /// </summary>
    public sealed class HakDirectoryCatalog : IHakResourceCatalog
    {
        private readonly Dictionary<ResourceIdentity, string> _index;

        public string DirectoryPath { get; }

        public string SourcePath => DirectoryPath;

        private HakDirectoryCatalog(
            string directoryPath,
            Dictionary<ResourceIdentity, string> index,
            DateTime contentVersionUtc)
        {
            DirectoryPath = directoryPath;
            _index = index;
            ContentVersionUtc = contentVersionUtc;
        }

        /// <summary>
        /// Total number of resources indexed from the folder.
        /// </summary>
        public int ResourceCount => _index.Count;

        public IEnumerable<ResourceIdentity> Resources => _index.Keys;

        /// <summary>
        /// Latest write time among indexed resources. Consumers may use this conservative layer
        /// version to invalidate derived artifacts whose exact transitive dependencies are unknown.
        /// </summary>
        public DateTime ContentVersionUtc { get; }

        /// <summary>
        /// Scan a hak source folder's file listing into a resref+type index. Files whose
        /// extension does not map to a known Aurora resource type (readmes, .gitkeep, etc.) are
        /// skipped rather than failing the scan.
        /// </summary>
        public static HakDirectoryCatalog Scan(string directoryPath)
        {
            var index = new Dictionary<ResourceIdentity, string>();
            var contentVersionUtc = DateTime.MinValue;

            foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly))
            {
                var resourceType = ResourceIdentity.TypeFromExtension(Path.GetExtension(file));
                if (resourceType == ResourceTypes.Invalid)
                    continue;

                var identity = new ResourceIdentity(Path.GetFileNameWithoutExtension(file), resourceType);

                // Last file wins on a duplicate resref+type within the same folder. This should
                // not normally happen (and is moot on Windows' case-insensitive filesystem), but
                // "last enumerated wins" keeps behavior defined rather than order-dependent-crash.
                index[identity] = file;
                contentVersionUtc = Max(contentVersionUtc, File.GetLastWriteTimeUtc(file));
            }

            return new HakDirectoryCatalog(directoryPath, index, contentVersionUtc);
        }

        private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;

        public bool TryGetPath(ResourceIdentity identity, out string path)
        {
            if (_index.TryGetValue(identity, out var found))
            {
                path = found;
                return true;
            }

            path = string.Empty;
            return false;
        }

        public string Describe(ResourceIdentity identity) =>
            TryGetPath(identity, out var path) ? path : DirectoryPath;

        public bool TryGetBytes(ResourceIdentity identity, out byte[] bytes)
        {
            if (TryGetPath(identity, out var path))
            {
                bytes = File.ReadAllBytes(path);
                return true;
            }

            bytes = Array.Empty<byte>();
            return false;
        }
    }
}
