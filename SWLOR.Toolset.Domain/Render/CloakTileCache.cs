namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Least-recently-used cache of cloak atlas tiles. Each tile otherwise costs a full
    /// 2048x4096 atlas decode; concurrent requests for one tile share a single decode.
    /// </summary>
    internal sealed class CloakTileCache
    {
        private readonly int _capacity;
        private readonly object _gate = new();
        private readonly Dictionary<string, LinkedListNode<KeyValuePair<string, Lazy<TextureImage?>>>> _entries =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly LinkedList<KeyValuePair<string, Lazy<TextureImage?>>> _order = new();

        public CloakTileCache(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
            _capacity = capacity;
        }

        public int Count
        {
            get
            {
                lock (_gate)
                    return _entries.Count;
            }
        }

        public TextureImage? GetOrLoad(string key, Func<TextureImage?> load)
        {
            Lazy<TextureImage?> tile;
            lock (_gate)
            {
                if (_entries.TryGetValue(key, out var node))
                {
                    _order.Remove(node);
                    _order.AddFirst(node);
                    tile = node.Value.Value;
                }
                else
                {
                    tile = new Lazy<TextureImage?>(load, LazyThreadSafetyMode.ExecutionAndPublication);
                    _entries[key] = _order.AddFirst(new KeyValuePair<string, Lazy<TextureImage?>>(key, tile));
                    if (_order.Count > _capacity)
                    {
                        var oldest = _order.Last!;
                        _order.RemoveLast();
                        _entries.Remove(oldest.Value.Key);
                    }
                }
            }

            // Decode outside the lock so other tiles stay available meanwhile.
            return tile.Value;
        }

        public void Clear()
        {
            lock (_gate)
            {
                _entries.Clear();
                _order.Clear();
            }
        }
    }
}
