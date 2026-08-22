using Avalonia.Media.Imaging;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// A bounded most-recently-used cache of decoded preview bitmaps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bound is the point. A 128px preview is 64 KB of pixels, and the module has some 17,000
    /// blueprints - keeping every one decoded would be more than a gigabyte of bitmaps to save re-reading
    /// PNGs that are already on disk. A builder looks at a few hundred tiles in a sitting, so a cap in
    /// the low thousands is indistinguishable from unbounded in practice.
    /// </para>
    /// <para>
    /// Evicted bitmaps are deliberately not disposed: a tile that is still on screen holds the same
    /// instance, and disposing underneath it would leave a hole in the grid. Dropping the reference is
    /// enough - the garbage collector reclaims it once nothing is drawing it.
    /// </para>
    /// </remarks>
    public sealed class BitmapMemoryCache
    {
        private readonly int _capacity;
        private readonly object _gate = new();
        private readonly Dictionary<string, LinkedListNode<Entry>> _entries;

        /// <summary>Most recent at the head.</summary>
        private readonly LinkedList<Entry> _order = new();

        public BitmapMemoryCache(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
            _entries = new Dictionary<string, LinkedListNode<Entry>>(capacity, StringComparer.OrdinalIgnoreCase);
        }

        public int Count
        {
            get { lock (_gate) return _entries.Count; }
        }

        /// <summary>
        /// Looks a key up. <paramref name="bitmap"/> is null for a key known to have no artwork, which is
        /// why the hit/miss answer is the return value rather than a null check.
        /// </summary>
        public bool TryGet(string key, out Bitmap? bitmap)
        {
            lock (_gate)
            {
                if (!_entries.TryGetValue(key, out var node))
                {
                    bitmap = null;
                    return false;
                }

                _order.Remove(node);
                _order.AddFirst(node);
                bitmap = node.Value.Bitmap;
                return true;
            }
        }

        /// <summary>Stores a bitmap, or null to remember that this key has none.</summary>
        public void Set(string key, Bitmap? bitmap)
        {
            lock (_gate)
            {
                if (_entries.TryGetValue(key, out var existing))
                {
                    _order.Remove(existing);
                    _entries.Remove(key);
                }

                _entries[key] = _order.AddFirst(new Entry(key, bitmap));

                while (_entries.Count > _capacity)
                {
                    var oldest = _order.Last;
                    if (oldest == null)
                        break;

                    _order.RemoveLast();
                    _entries.Remove(oldest.Value.Key);
                }
            }
        }

        /// <summary>Forgets one key, so the next request for it re-renders rather than reusing a stale image.</summary>
        public void Remove(string key)
        {
            lock (_gate)
            {
                if (!_entries.TryGetValue(key, out var node))
                    return;

                _order.Remove(node);
                _entries.Remove(key);
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _entries.Clear();
                _order.Clear();
            }
        }

        private readonly record struct Entry(string Key, Bitmap? Bitmap);
    }
}
