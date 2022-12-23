using System.Collections.Generic;
using System.Text;
using NRediSearch;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.DBService
{
    public class DBQuery<T>
        where T: EntityBase
    {
        private class SearchCriteria
        {
            public string Text { get; set; }
            public bool SkipEscaping { get; set; }

            public SearchCriteria(string text)
            {
                Text = text;
                SkipEscaping = false;
            }
        }

        private Dictionary<string, SearchCriteria> FieldSearches { get; }
        private int Offset { get; set; }
        private int Limit { get; set; }
        private string SortByField { get; set; }
        private bool IsAscending { get; set; }

        /// <summary>
        /// Adds a filter based on a field's name for the given text.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The text to search for</param>
        /// <param name="allowPartialMatches">If true, partial matches are accepted.</param>
        /// <returns>A configured DBQuery</returns>
        public DBQuery<T> AddFieldSearch(string fieldName, string search, bool allowPartialMatches)
        {
            if (allowPartialMatches)
                search += "*";

            FieldSearches.Add(fieldName, new SearchCriteria(search));

            return this;
        }

        /// <summary>
        /// Adds a filter based on a field's name for the given text.
        /// Will search for any matches in the provided list of integers.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The list of Ids to search for</param>
        /// <returns>A configured DBQuery</returns>
        public DBQuery<T> AddFieldSearch(string fieldName, IEnumerable<int> search)
        {
            var searchText = string.Join("|", search);
            var criteria = new SearchCriteria(searchText)
            {
                SkipEscaping = true
            };

            FieldSearches.Add(fieldName, criteria);

            return this;
        }

        /// <summary>
        /// Adds a filter based on a field's name for the given text.
        /// Will search for any matches in the provided list of strings.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The list of values to search for</param>
        /// <returns>A configured DBQuery</returns>
        public DBQuery<T> AddFieldSearch(string fieldName, IEnumerable<string> search)
        {
            var list = new List<string>();
            foreach (var s in search)
            {
                list.Add(DB.EscapeTokens(s));
            }

            var searchText = string.Join("|", list);
            var criteria = new SearchCriteria(searchText)
            {
                SkipEscaping = true
            };

            FieldSearches.Add(fieldName, criteria);

            return this;
        }

        /// <summary>
        /// Adds a filter based on a field's name for the given number.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The number to search for</param>
        /// <returns>A configured DBQuery</returns>
        public DBQuery<T> AddFieldSearch(string fieldName, int search)
        {
            FieldSearches.Add(fieldName, new SearchCriteria(search.ToString()));

            return this;
        }

        /// <summary>
        /// Adds a filter based on a field's name for the given boolean.
        /// </summary>
        /// <param name="fieldName">The name of the field to search for</param>
        /// <param name="search">The value to search for</param>
        /// <returns>A configured DBQuery</returns>
        public DBQuery<T> AddFieldSearch(string fieldName, bool search)
        {
            FieldSearches.Add(fieldName, new SearchCriteria((search ? 1 : 0).ToString()));

            return this;
        }

        /// <summary>
        /// Determines the number of records and the number of records to skip.
        /// </summary>
        /// <param name="limit">The number of records to retrieve.</param>
        /// <param name="offset">The number of records to skip.</param>
        /// <returns>A configured DBQuery</returns>
        public DBQuery<T> AddPaging(int limit, int offset)
        {
            Limit = limit;
            Offset = offset;

            return this;
        }

        /// <summary>
        /// Orders the result set by a given field name.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="isAscending">if true, sort will be in ascending order. Otherwise, descending order will be used.</param>
        /// <returns>A configured DBQuery</returns>
        public DBQuery<T> OrderBy(string fieldName, bool isAscending = true)
        {
            SortByField = fieldName;
            IsAscending = isAscending;

            return this;
        }

        /// <summary>
        /// Builds an NRedisSearch query.
        /// </summary>
        /// <returns>An NRedisSearch query.</returns>
        public Query BuildQuery(bool countsOnly = false)
        {
            var sb = new StringBuilder();

            // Exact filter on this type of entity.
            sb.Append($"@EntityType:\"{typeof(T).Name}\"");

            // Filter by name/searchText
            foreach (var (name, criteria) in FieldSearches)
            {
                var search = criteria.SkipEscaping
                    ? criteria.Text
                    : DB.EscapeTokens(criteria.Text);

                sb.Append($" @{name}:{search}");
            }

            var query = new Query(sb.ToString());

            // If we're only retrieving the number of records this query will return
            // (before pagination is applied), set the limit to zero records.
            if (countsOnly)
            {
                query.Limit(0, 0);
            }
            else
            {
                // Apply pagination
                if (Limit > 0)
                {
                    query.Limit(Offset, Limit);
                }
                // If no limit is specified, default to 50.
                // The default limit is 10 which is too small for our use case.
                else
                {
                    query.Limit(0, 50);
                }
            }

            if (!string.IsNullOrWhiteSpace(SortByField))
            {
                query.SetSortBy(SortByField, IsAscending);
            }

            return query;
        }

        public DBQuery()
        {
            FieldSearches = new Dictionary<string, SearchCriteria>();
        }
    }
}
