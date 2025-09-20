using NRediSearch;

namespace SWLOR.Shared.Abstractions.Contracts
{
    /// <summary>
    /// Interface for building database queries with search criteria, pagination, and sorting.
    /// </summary>
    /// <typeparam name="T">The type of entity to query</typeparam>
    public interface IDBQuery<T> where T : EntityBase
    {
        /// <summary>
        /// Adds a filter based on a field's name for the given text.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The text to search for</param>
        /// <param name="allowPartialMatches">If true, partial matches are accepted.</param>
        /// <returns>A configured DBQuery</returns>
        IDBQuery<T> AddFieldSearch(string fieldName, string search, bool allowPartialMatches);

        /// <summary>
        /// Adds a filter based on a field's name for the given text.
        /// Will search for any matches in the provided list of integers.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The list of Ids to search for</param>
        /// <returns>A configured DBQuery</returns>
        IDBQuery<T> AddFieldSearch(string fieldName, IEnumerable<int> search);

        /// <summary>
        /// Adds a filter based on a field's name for the given text.
        /// Will search for any matches in the provided list of strings.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The list of values to search for</param>
        /// <returns>A configured DBQuery</returns>
        IDBQuery<T> AddFieldSearch(string fieldName, IEnumerable<string> search);

        /// <summary>
        /// Adds a filter based on a field's name for the given number.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The number to search for</param>
        /// <returns>A configured DBQuery</returns>
        IDBQuery<T> AddFieldSearch(string fieldName, int search);

        /// <summary>
        /// Adds a filter based on a field's name for the given boolean.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The value to search for</param>
        /// <returns>A configured DBQuery</returns>
        IDBQuery<T> AddFieldSearch(string fieldName, bool search);

        /// <summary>
        /// Determines the number of records and the number of records to skip.
        /// </summary>
        /// <param name="limit">The number of records to retrieve.</param>
        /// <param name="offset">The number of records to skip.</param>
        /// <returns>A configured DBQuery</returns>
        IDBQuery<T> AddPaging(int limit, int offset);

        /// <summary>
        /// Orders the result set by a given field name.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="isAscending">if true, sort will be in ascending order. Otherwise, descending order will be used.</param>
        /// <returns>A configured DBQuery</returns>
        IDBQuery<T> OrderBy(string fieldName, bool isAscending = true);

        Query BuildQuery(bool countsOnly = false);
    }
}
