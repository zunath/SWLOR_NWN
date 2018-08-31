using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Extension
{
    public static class EnumerableExtensions
    {
        public static bool ContainsAll<T>(this IEnumerable<T> source, IEnumerable<T> inner)
        {
            return inner.All(source.Contains);
        }

        public static bool Contains<T>(this IEnumerable<T> source, IEnumerable<T> inner, IEqualityComparer<T> comparer)
        {
            return inner.All(element => source.Contains(element, comparer));
        }
    }
}
