using System.Collections.Generic;

namespace SWLOR.Game.Server.Extension
{
    internal static class CollectionExtensions
    {
        public static void InsertOrdered<T>(this List<T> sortedList, T item, IComparer<T> comparer = null)
        {
            var binaryIndex = sortedList.BinarySearch(item, comparer);
            var index = binaryIndex < 0 ? ~binaryIndex : binaryIndex;
            sortedList.Insert(index, item);
        }
    }
}
