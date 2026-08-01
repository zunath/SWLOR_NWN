using System.Collections.Concurrent;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Resolves and parses tile walkmeshes ("&lt;tileModelResRef&gt;.wok") by resref through a
    /// <see cref="ResourceIndex"/>, caching both hits and misses so a 438-area batch assembly
    /// parses each distinct tile's walkmesh at most once. Mirrors <see cref="TileModelCache"/>
    /// exactly - callers own an instance and share it across every <see cref="AreaSceneBuilder.Build"/>
    /// call in a batch.
    /// </summary>
    public sealed class TileWalkmeshCache
    {
        private static readonly ushort WokResourceType = ResourceIdentity.TypeFromExtension("wok");

        private readonly ResourceIndex _resourceIndex;
        private readonly Func<Func<int, bool>> _buildWalkability;
        private Func<int, bool> _isWalkable;
        private readonly ConcurrentDictionary<string, WalkMesh?> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        /// <param name="resourceIndex">Resolves "&lt;resref&gt;.wok" resources, hak-over-base-game precedence.</param>
        /// <param name="isWalkable">Classifies a face's surfacemat.2da row id as walkable, passed through to <see cref="WokMeshLoader.Parse"/>.</param>
        public TileWalkmeshCache(ResourceIndex resourceIndex, Func<int, bool> isWalkable)
            : this(resourceIndex, () => isWalkable)
        {
        }

        /// <param name="resourceIndex">Resolves "&lt;resref&gt;.wok" resources, hak-over-base-game precedence.</param>
        /// <param name="buildWalkability">
        /// Rebuilds the surfacemat.2da classifier after a resource reload, once TwoDaService has
        /// invalidated its table cache.
        /// </param>
        public TileWalkmeshCache(
            ResourceIndex resourceIndex,
            Func<Func<int, bool>> buildWalkability)
        {
            _resourceIndex = resourceIndex ?? throw new ArgumentNullException(nameof(resourceIndex));
            _buildWalkability = buildWalkability ?? throw new ArgumentNullException(nameof(buildWalkability));
            _isWalkable = _buildWalkability();
            _resourceIndex.ResourcesReloaded += OnResourcesReloaded;
        }

        private void OnResourcesReloaded()
        {
            Volatile.Write(ref _isWalkable, _buildWalkability());
            _cache.Clear();
        }

        /// <summary>
        /// Resolves and parses a tile's walkmesh by its model resref (the walkmesh resource
        /// shares the tile model's resref, with a ".wok" extension instead of ".mdl"). Returns
        /// null (and remembers the null result) when the resref is blank, the resource can't be
        /// found through the index, or parsing throws - this method itself never throws, so a
        /// missing/unparseable walkmesh degrades to "no ground-snap data for this tile" instead
        /// of aborting an area (or a whole batch).
        /// </summary>
        public WalkMesh? GetOrBuild(string? tileModelResRef)
        {
            if (string.IsNullOrWhiteSpace(tileModelResRef))
                return null;

            return _cache.GetOrAdd(tileModelResRef, Build);
        }

        private WalkMesh? Build(string tileModelResRef)
        {
            try
            {
                var identity = new ResourceIdentity(tileModelResRef, WokResourceType);
                if (!_resourceIndex.TryLookup(identity, out var handle))
                    return null;

                var bytes = handle.GetBytes();
                if (bytes.Length == 0)
                    return null;

                return WokMeshLoader.Parse(bytes, Volatile.Read(ref _isWalkable));
            }
            catch (Exception)
            {
                // Missing/unparseable walkmeshes must never abort area assembly - the caller
                // treats a null result as "no ground-snap data for this tile" and keeps going.
                return null;
            }
        }
    }
}
