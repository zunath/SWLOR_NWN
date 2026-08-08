using System.Collections.Concurrent;
using SWLOR.NWN.Formats.Mdl;
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
        private readonly ConcurrentDictionary<string, RenderModel?> _cache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, RenderModel?> _placeablePreviewCache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, RenderModel?> _doorTransitionCache =
            new(StringComparer.OrdinalIgnoreCase);

        public TileModelCache(ResourceIndex resourceIndex)
        {
            _resourceIndex = resourceIndex ?? throw new ArgumentNullException(nameof(resourceIndex));
            _resourceIndex.ResourcesReloaded += Clear;
        }

        public void Clear()
        {
            _cache.Clear();
            _placeablePreviewCache.Clear();
            _doorTransitionCache.Clear();
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

        /// <summary>
        /// Resolves the same geometry as <see cref="GetOrBuild"/>, plus transform-only placeable
        /// animation tracks and emitter metadata for the single-model preview. Kept in a separate
        /// cache so tile and area models do not pay for animation states they never continuously play.
        /// </summary>
        public RenderModel? GetOrBuildPlaceablePreview(string? modelResRef)
        {
            if (string.IsNullOrWhiteSpace(modelResRef))
                return null;

            return _placeablePreviewCache.GetOrAdd(modelResRef, BuildPlaceablePreview);
        }

        /// <summary>
        /// Resolves an invisible transition-door model including its <c>render 0</c> editor
        /// selection surfaces. Kept separate from the ordinary cache so the same MDL remains
        /// invisible when used as normal game artwork.
        /// </summary>
        public RenderModel? GetOrBuildDoorTransition(string? modelResRef)
        {
            if (string.IsNullOrWhiteSpace(modelResRef))
                return null;

            return _doorTransitionCache.GetOrAdd(modelResRef, BuildDoorTransition);
        }

        private RenderModel? Build(string modelResRef)
        {
            var model = Load(modelResRef);
            return model == null ? null : MdlMeshBuilder.Build(model);
        }

        private RenderModel? BuildPlaceablePreview(string modelResRef)
        {
            var model = Load(modelResRef);
            return model == null ? null : MdlMeshBuilder.BuildPlaceablePreview(model);
        }

        private RenderModel? BuildDoorTransition(string modelResRef)
        {
            var model = Load(modelResRef);
            if (model == null)
                return null;

            try
            {
                return MdlMeshBuilder.BuildDoorTransition(model);
            }
            catch (Exception)
            {
                // Hidden editor meshes are less constrained than runtime artwork. A malformed
                // parent chain or other mesh-build failure must fall back to the fixed transition
                // doorway instead of aborting assembly of the entire area.
                return null;
            }
        }

        private MdlModel? Load(string modelResRef)
        {
            try
            {
                var identity = new ResourceIdentity(modelResRef, MdlResourceType);
                if (!_resourceIndex.TryLookup(identity, out var handle))
                    return null;

                var bytes = handle.GetBytes();
                if (bytes.Length == 0)
                    return null;

                // A reader per parse. MdlReader is not reentrant - MdlBinaryReader keeps the file's
                // data block, its pointer base and the live BinaryReader in fields - and this method
                // runs from ConcurrentDictionary.GetOrAdd, which deliberately does NOT serialise its
                // factory. One shared reader therefore had two threads parsing two tiles into each
                // other's state, which is not a crash but a corruption: vertices read at another
                // model's pointer base come back as enormous garbage triangles, mesh headers land on
                // the wrong bytes so textures resolve to nothing, and a read past the end throws and
                // caches a null - the tile then draws as a magenta placeholder. cz220shipbreakin
                // showed all three at once (see TileModelCacheConcurrencyTests).
                return new MdlReader().Parse(bytes);
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
