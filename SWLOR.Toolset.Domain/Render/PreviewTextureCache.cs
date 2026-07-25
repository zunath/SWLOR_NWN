using SWLOR.Toolset.Domain.GameData.Resources;

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

        private readonly ResourceIndex _resourceIndex;
        private readonly long _budgetBytes;

        private readonly object _gate = new();
        private readonly Dictionary<string, LinkedListNode<Entry>> _entries =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Most recently used at the head.</summary>
        private readonly LinkedList<Entry> _order = new();

        private long _heldBytes;

        public PreviewTextureCache(ResourceIndex resourceIndex, long budgetBytes = DefaultBudgetBytes)
        {
            if (budgetBytes <= 0) throw new ArgumentOutOfRangeException(nameof(budgetBytes));

            _resourceIndex = resourceIndex ?? throw new ArgumentNullException(nameof(resourceIndex));
            _budgetBytes = budgetBytes;
        }

        /// <summary>Decoded pixels held right now, for diagnostics.</summary>
        public long HeldBytes
        {
            get { lock (_gate) return _heldBytes; }
        }

        /// <summary>
        /// The decoded diffuse texture for a mesh's bitmap or material name, or null when it does not
        /// resolve. Safe to call from several render threads at once; never throws.
        /// </summary>
        public TextureImage? Get(string? textureOrMaterialName)
        {
            if (string.IsNullOrWhiteSpace(textureOrMaterialName))
                return null;

            lock (_gate)
            {
                if (_entries.TryGetValue(textureOrMaterialName, out var node))
                {
                    _order.Remove(node);
                    _order.AddFirst(node);
                    return node.Value.Texture;
                }
            }

            var decoded = Decode(textureOrMaterialName);

            lock (_gate)
            {
                if (_entries.TryGetValue(textureOrMaterialName, out var existing))
                    return existing.Value.Texture; // Another thread decoded it first; keep one copy.

                var size = decoded?.Pixels.LongLength ?? 0;
                _entries[textureOrMaterialName] = _order.AddFirst(new Entry(textureOrMaterialName, decoded, size));
                _heldBytes += size;

                while (_heldBytes > _budgetBytes && _order.Count > 1)
                {
                    var oldest = _order.Last;
                    if (oldest == null)
                        break;

                    _order.RemoveLast();
                    _entries.Remove(oldest.Value.Key);
                    _heldBytes -= oldest.Value.SizeBytes;
                }

                return decoded;
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _entries.Clear();
                _order.Clear();
                _heldBytes = 0;
            }
        }

        /// <summary>
        /// Resolves through any .mtr material override first - a mesh's "texture" is sometimes a material
        /// name whose real diffuse map is declared inside it - then decodes TGA, DDS or PLT.
        /// </summary>
        private TextureImage? Decode(string textureOrMaterialName)
        {
            try
            {
                var diffuse = MaterialResolver.ResolveDiffuseTextureName(_resourceIndex, textureOrMaterialName);
                return TextureLoader.Load(_resourceIndex, diffuse);
            }
            catch (Exception)
            {
                // A malformed texture is a flat-shaded mesh, not a failed thumbnail.
                return null;
            }
        }

        private readonly record struct Entry(string Key, TextureImage? Texture, long SizeBytes);
    }
}
