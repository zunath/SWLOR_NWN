using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Nwn.Preview.Cache;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Decodes diffuse textures for thumbnail rendering, keeping a byte-budgeted cache of the results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Texture reuse across a module is heavy - one tileset texture can serve hundreds of placeables - so
    /// caching is worth a great deal here. The budget is measured in bytes rather than entries because
    /// the sizes are wildly uneven: an icon is 4 KB and a 1024-square wall texture is 4 MB, so a
    /// count-based cap that looks modest can still hold most of a gigabyte.
    /// </para>
    /// <para>
    /// Misses are remembered too. A missing texture costs a lookup across 113 hak layers plus the base
    /// game, and models reference plenty of textures that were never shipped.
    /// </para>
    /// </remarks>
    public sealed class PreviewTextureCache
    {
        /// <summary>Default budget for decoded pixels. Roughly sixteen 1024-square textures.</summary>
        public const long DefaultBudgetBytes = 64L * 1024 * 1024;
        public const int DefaultMaximumEntries = 4096;

        private readonly ResourceIndex _resourceIndex;
        private readonly BoundedLruCache<string, TextureImage> _cache;

        public PreviewTextureCache(
            ResourceIndex resourceIndex,
            long budgetBytes = DefaultBudgetBytes,
            int maximumEntries = DefaultMaximumEntries)
        {
            if (budgetBytes <= 0) throw new ArgumentOutOfRangeException(nameof(budgetBytes));

            _resourceIndex = resourceIndex ?? throw new ArgumentNullException(nameof(resourceIndex));
            _cache = new BoundedLruCache<string, TextureImage>(
                budgetBytes,
                maximumEntries,
                texture => texture?.Pixels.LongLength ?? 0,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Decoded pixels held right now, for diagnostics.</summary>
        public long HeldBytes
        {
            get => _cache.HeldBytes;
        }

        /// <summary>
        /// The decoded diffuse texture for a mesh's bitmap or material name, or null when it does not
        /// resolve. Safe to call from several render threads at once; never throws.
        /// </summary>
        public TextureImage? Get(
            string? textureOrMaterialName,
            IReadOnlyDictionary<int, int>? layerColorIndices = null,
            IReadOnlyDictionary<string, int>? tintMapOverrides = null,
            bool resolveMaterial = true,
            AppearanceArmor armorPart = AppearanceArmor.Invalid)
        {
            if (string.IsNullOrWhiteSpace(textureOrMaterialName))
                return null;

            var key = CacheKey(
                textureOrMaterialName,
                layerColorIndices,
                tintMapOverrides,
                resolveMaterial,
                armorPart);
            return _cache.GetOrAdd(key, () => Decode(
                textureOrMaterialName,
                layerColorIndices,
                tintMapOverrides,
                resolveMaterial,
                armorPart));
        }

        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Resolves through any .mtr material override first - a mesh's "texture" is sometimes a material
        /// name whose real diffuse map is declared inside it - then decodes TGA, DDS or PLT.
        /// </summary>
        private TextureImage? Decode(
            string textureOrMaterialName,
            IReadOnlyDictionary<int, int>? layerColorIndices,
            IReadOnlyDictionary<string, int>? tintMapOverrides,
            bool resolveMaterial,
            AppearanceArmor armorPart)
        {
            try
            {
                var material = resolveMaterial
                    ? MaterialResolver.TryParseMaterial(_resourceIndex, textureOrMaterialName)
                    : null;
                if (material != null &&
                    TintMapTextureRenderer.Render(
                        _resourceIndex,
                        textureOrMaterialName,
                        material,
                        layerColorIndices,
                        tintMapOverrides,
                        armorPart) is { } tintMap)
                {
                    return tintMap;
                }

                var diffuse = MaterialResolver.ResolveDiffuseTextureName(
                    _resourceIndex,
                    textureOrMaterialName,
                    resolveMaterial);
                return TextureLoader.Load(_resourceIndex, diffuse, layerColorIndices);
            }
            catch (Exception)
            {
                // A malformed texture is a flat-shaded mesh, not a failed thumbnail.
                return null;
            }
        }

        internal static string CacheKey(
            string textureOrMaterialName,
            IReadOnlyDictionary<int, int>? layerColorIndices,
            IReadOnlyDictionary<string, int>? tintMapOverrides = null,
            bool resolveMaterial = true,
            AppearanceArmor armorPart = AppearanceArmor.Invalid)
        {
            if ((layerColorIndices == null || layerColorIndices.Count == 0) &&
                (tintMapOverrides == null || tintMapOverrides.Count == 0))
                return $"{(resolveMaterial ? 'm' : 't')}|{textureOrMaterialName}|p:{(int)armorPart}";

            var layers = layerColorIndices == null
                ? string.Empty
                : string.Join(",", layerColorIndices.OrderBy(pair => pair.Key)
                    .Select(pair => $"{pair.Key}:{pair.Value}"));
            var overrides = tintMapOverrides == null
                ? string.Empty
                : string.Join(",", tintMapOverrides.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => $"{pair.Key}:{pair.Value}"));
            return $"{(resolveMaterial ? 'm' : 't')}|{textureOrMaterialName}|p:{(int)armorPart}|{layers}|{overrides}";
        }

    }
}
