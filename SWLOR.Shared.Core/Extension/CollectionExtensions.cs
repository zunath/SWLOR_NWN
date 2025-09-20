namespace SWLOR.Shared.Core.Extension
{
    internal static class CollectionExtensions
    {
        public static void InsertOrdered<T>(this List<T> sortedList, T item, IComparer<T> comparer = null)
        {
            var binaryIndex = sortedList.BinarySearch(item, comparer);
            var index = binaryIndex < 0 ? ~binaryIndex : binaryIndex;
            sortedList.Insert(index, item);
        }

        public static void AddElement<TKey, TValue>(this IDictionary<TKey, List<TValue>> mutableLookup, TKey key, TValue value)
        {
            if (!mutableLookup.TryGetValue(key, out var values))
            {
                values = new List<TValue>();
                mutableLookup[key] = values;
            }

            values.Add(value);
        }

        public static TValue SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out var retVal) ? retVal : default;
        }
    }
}