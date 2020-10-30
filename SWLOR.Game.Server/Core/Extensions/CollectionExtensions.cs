using System.Collections.Generic;

namespace SWLOR.Game.Server.Core.Extensions
{
    internal static class CollectionExtensions
    {
        public static void InsertOrdered<T>(this List<T> sortedList, T item, IComparer<T> comparer = null)
        {
            int binaryIndex = sortedList.BinarySearch(item, comparer);
            int index = binaryIndex < 0 ? ~binaryIndex : binaryIndex;
            sortedList.Insert(index, item);
        }

        public static void AddElement<TKey, TValue>(this IDictionary<TKey, List<TValue>> mutableLookup, TKey key, TValue value)
        {
            if (!mutableLookup.TryGetValue(key, out List<TValue> values))
            {
                values = new List<TValue>();
                mutableLookup[key] = values;
            }

            values.Add(value);
        }

        public static TValue SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out TValue retVal) ? retVal : default;
        }
    }
}