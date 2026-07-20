using System.Collections.Concurrent;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Editor lookup over the full tileset (.set) corpus, resolved through a
    /// <see cref="Resources.ResourceIndex"/> rather than a fixed directory: tileset names are
    /// discovered by scanning every hak layer's directory listing for "*.set" files (the
    /// <see cref="ResourceIndex"/> itself only exposes lookup-by-identity, not enumeration, so this
    /// walks <see cref="ResourceIndex.HakLayers"/> directly for discovery - resolution of a
    /// specific tileset's bytes still goes through <see cref="ResourceIndex.TryLookup"/> so
    /// hak-precedence "later wins" is honored the same way every other resource lookup gets it).
    /// Each named tileset is parsed via <see cref="SetFileParser"/> at most once and cached for
    /// the lifetime of the catalog. An area document's Tileset field (e.g. "tde01") is the resref
    /// to pass to <see cref="TryGetTileset"/>.
    /// </summary>
    public sealed class TilesetCatalog
    {
        private static readonly ushort SetResourceType = ResourceIdentity.TypeFromExtension("set");

        private readonly ResourceIndex _resourceIndex;
        private readonly Lazy<IReadOnlyList<string>> _names;
        private readonly ConcurrentDictionary<string, Lazy<TilesetDefinition?>> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        public TilesetCatalog(ResourceIndex resourceIndex)
        {
            _resourceIndex = resourceIndex ?? throw new ArgumentNullException(nameof(resourceIndex));
            _names = new Lazy<IReadOnlyList<string>>(DiscoverNames);
        }

        /// <summary>
        /// All tileset resrefs visible across every hak layer (deduplicated, case-insensitive),
        /// sorted for stable/deterministic output. Discovery is a one-time directory scan, cached
        /// for the lifetime of the catalog.
        /// </summary>
        public IReadOnlyList<string> GetTilesetNames() => _names.Value;

        /// <summary>
        /// Attempts to resolve and parse a tileset by resref (e.g. "tde01", matching an area
        /// document's Tileset field). Parsing is lazy and cached: the first successful or failed
        /// attempt for a given resref is remembered rather than re-parsed on every call.
        /// </summary>
        public bool TryGetTileset(string resref, out TilesetDefinition tileset)
        {
            if (string.IsNullOrWhiteSpace(resref))
            {
                tileset = null!;
                return false;
            }

            var lazy = _cache.GetOrAdd(resref, key => new Lazy<TilesetDefinition?>(() =>
            {
                var identity = new ResourceIdentity(key, SetResourceType);
                if (!_resourceIndex.TryLookup(identity, out var handle))
                    return null;

                var bytes = handle.GetBytes();
                return bytes.Length == 0 ? null : SetFileParser.Parse(bytes);
            }));

            var result = lazy.Value;
            tileset = result!;
            return result != null;
        }

        private IReadOnlyList<string> DiscoverNames()
        {
            _resourceIndex.EnsureInitialized();

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var layer in _resourceIndex.HakLayers)
            {
                if (!Directory.Exists(layer.DirectoryPath))
                    continue;

                foreach (var file in Directory.EnumerateFiles(layer.DirectoryPath, "*.set"))
                    names.Add(Path.GetFileNameWithoutExtension(file));
            }

            return names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
