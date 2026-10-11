using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.DBService
{
    /// <summary>
    /// Builds the RediSearch text for a partial-match field search so every word must match
    /// the start of a word in that field. Stop words are ignored.
    /// </summary>
    public static class PartialMatchSearchText
    {
        // RediSearch rejects prefixes shorter than its default MINPREFIX.
        private const int MinimumPrefixLength = 2;

        // RediSearch's default stop words are removed from both the index and the query, so no search can match them.
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "is", "the", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "it",
            "no", "not", "of", "on", "or", "such", "that", "their", "then", "there", "these", "they", "this", "to",
            "was", "will", "with"
        };

        /// <summary>
        /// Returns the escaped query text for a partial match on the given search.
        /// "Per Tes" becomes "(Per* Tes*)"; one-character words stay exact and stop words are dropped.
        /// </summary>
        /// <param name="search">The text the player or DM typed.</param>
        public static string Build(string search)
        {
            var words = (search ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var searchable = words.Where(word => !StopWords.Contains(word)).ToList();

            // Without a prefixable word the group could be empty or match every record, so keep the single-prefix form.
            if (!searchable.Any(word => word.Length >= MinimumPrefixLength))
                return DB.EscapeTokens(string.Join(" ", words)) + "*";

            var terms = searchable.Select(word => word.Length >= MinimumPrefixLength
                ? DB.EscapeTokens(word) + "*"
                : DB.EscapeTokens(word));

            return "(" + string.Join(" ", terms) + ")";
        }
    }
}
