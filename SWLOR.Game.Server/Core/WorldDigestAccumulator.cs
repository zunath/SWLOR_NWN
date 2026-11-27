using System.Globalization;

namespace SWLOR.Game.Server.Core
{
    /// <summary>One digest of <see cref="WorldDigest"/>: a count and an FNV-1a hash of the values added, in order.</summary>
    internal sealed class WorldDigestAccumulator
    {
        private const ulong Prime = 1099511628211UL;
        private readonly string _name;
        private ulong _hash = 14695981039346656037UL;
        private long _count;

        public WorldDigestAccumulator(string name)
        {
            _name = name;
        }

        public void Add(long count, params string[] values)
        {
            _count += count;
            foreach (var value in values)
            {
                foreach (var character in value ?? string.Empty)
                {
                    _hash ^= character;
                    _hash *= Prime;
                }

                _hash ^= 0xFF;
                _hash *= Prime;
            }
        }

        public ReloadDigestLine Line()
        {
            return new ReloadDigestLine(ReloadDigests.WorldKind, _name, _count, _hash.ToString("x16", CultureInfo.InvariantCulture));
        }
    }
}
