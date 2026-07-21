using System.Collections.Concurrent;
using Radoub.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Resolves and parses tile MDL models by resref through a <see cref="ResourceIndex"/>,
    /// caching both hits and misses so a 438-area batch assembly (thousands of tile placements,
    /// mostly repeating the same handful of models per tileset) parses each distinct model at most
    /// once. Callers own an instance and share it across every <see cref="AreaSceneBuilder.Build"/>
    /// call in a batch; <see cref="TilePlacement"/>s across areas then reference the same
    /// <see cref="RenderModel"/> object rather than each holding their own copy.
    /// </summary>
    public sealed class TileModelCache
    {
        private static readonly ushort MdlResourceType = ResourceIdentity.TypeFromExtension("mdl");

        private readonly ResourceIndex _resourceIndex;
        private readonly MdlReader _reader = new();
        private readonly ConcurrentDictionary<string, RenderModel?> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        public TileModelCache(ResourceIndex resourceIndex)
        {
            _resourceIndex = resourceIndex ?? throw new ArgumentNullException(nameof(resourceIndex));
        }

        /// <summary>
        /// Resolves and parses a tile model by resref. Returns null (and remembers the null result)
        /// when the resref is blank, the resource can't be found through the index, or parsing/mesh
        /// building throws - this method itself never throws, so a bad model degrades to a fallback
        /// placement instead of aborting an area (or a whole batch).
        /// </summary>
        public RenderModel? GetOrBuild(string? modelResRef)
        {
            if (string.IsNullOrWhiteSpace(modelResRef))
                return null;

            return _cache.GetOrAdd(modelResRef, Build);
        }

        private RenderModel? Build(string modelResRef)
        {
            try
            {
                var identity = new ResourceIdentity(modelResRef, MdlResourceType);
                if (!_resourceIndex.TryLookup(identity, out var handle))
                    return null;

                var bytes = handle.GetBytes();
                if (bytes.Length == 0)
                    return null;

                var model = _reader.Parse(bytes);
                return MdlMeshBuilder.Build(model);
            }
            catch (Exception)
            {
                // Missing/unparseable tile models must never abort area assembly - the caller
                // treats a null result as "use a fallback placeholder" and keeps going.
                return null;
            }
        }
    }
}
