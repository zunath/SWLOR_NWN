using System.Collections.Concurrent;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Editor lookup over the full tileset (.set) corpus, resolved through a
    /// <see cref="Resources.ResourceIndex"/> rather than a fixed directory: tileset names are
    /// discovered through <see cref="ResourceIndex.EnumerateResources"/>, which merges base-game
    /// KEY/BIF identities with every hak layer. Resolution of a specific tileset's bytes still
    /// goes through <see cref="ResourceIndex.TryLookup"/> so hak-precedence "first wins" is
    /// honored the same way every other resource lookup gets it.
    /// Each named tileset is parsed via <see cref="SetFileParser"/> at most once and cached for
    /// the lifetime of the catalog. An area document's Tileset field (e.g. "tde01") is the resref
    /// to pass to <see cref="TryGetTileset"/>.
    /// </summary>
    public sealed class TilesetCatalog
    {
        private static readonly ushort SetResourceType = ResourceIdentity.TypeFromExtension("set");

        private readonly ResourceIndex _resourceIndex;
        private Lazy<IReadOnlyList<string>> _names;
        private readonly ConcurrentDictionary<string, Lazy<TilesetDefinition?>> _cache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _displayNames =
            new(StringComparer.OrdinalIgnoreCase);

        public TilesetCatalog(ResourceIndex resourceIndex)
        {
            _resourceIndex = resourceIndex ?? throw new ArgumentNullException(nameof(resourceIndex));
            _names = new Lazy<IReadOnlyList<string>>(DiscoverNames);
            _resourceIndex.ResourcesReloaded += Invalidate;
        }

        /// <summary>
        /// All tileset resrefs visible across the base game and every hak layer (deduplicated,
        /// case-insensitive), sorted for stable/deterministic output. Discovery is cached for the
        /// lifetime of the catalog.
        /// </summary>
        public IReadOnlyList<string> GetTilesetNames() => _names.Value;

        /// <summary>
        /// A human-readable label for a tileset picker: "ztd01 - [CEP] Desert". The name comes from
        /// the .set's [GENERAL] block, preferring UnlocalizedName ("[CEP] Desert") over the internal
        /// Name ("ZTD01"), which is usually just the resref in caps and adds nothing. Falls back to
        /// the bare resref when the file declares no useful name or cannot be read. Uses a
        /// header-only read (<see cref="SetFileParser.ParseHeader"/>), so labelling the whole
        /// tileset list does not parse ~16 MB of tile tables.
        /// </summary>
        public string GetDisplayLabel(string resref)
        {
            var name = GetDisplayName(resref);
            return string.IsNullOrEmpty(name) ? resref : $"{resref} - {name}";
        }

        /// <summary>
        /// The tileset's human-readable name alone (no resref prefix), or an empty string when the
        /// .set declares nothing better than its own resref. Cached per resref.
        /// </summary>
        public string GetDisplayName(string resref)
        {
            if (string.IsNullOrWhiteSpace(resref))
                return string.Empty;

            return _displayNames.GetOrAdd(resref, key =>
            {
                try
                {
                    var identity = new ResourceIdentity(key, SetResourceType);
                    if (!_resourceIndex.TryLookup(identity, out var handle))
                        return string.Empty;

                    var header = SetFileParser.ParseHeader(handle.GetBytes());
                    if (!string.IsNullOrWhiteSpace(header.UnlocalizedName))
                        return header.UnlocalizedName.Trim();

                    // The internal Name is normally just the resref in caps; only useful when it
                    // actually differs from the resref.
                    return !string.IsNullOrWhiteSpace(header.Name)
                           && !header.Name.Equals(key, StringComparison.OrdinalIgnoreCase)
                        ? header.Name.Trim()
                        : string.Empty;
                }
                catch
                {
                    return string.Empty; // an unreadable tileset just shows its resref
                }
            });
        }

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
            return _resourceIndex.EnumerateResources(SetResourceType)
                .Select(identity => identity.ResRef)
                .ToArray();
        }

        private void Invalidate()
        {
            _names = new Lazy<IReadOnlyList<string>>(DiscoverNames);
            _cache.Clear();
            _displayNames.Clear();
        }
    }
}
