using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.DBService
{
    /// <summary>
    /// Builds the RediSearch text for a partial-match field search so every word must match
    /// the start of a word in that field.
    /// </summary>
    public static class PartialMatchSearchText
    {
        // RediSearch rejects prefixes shorter than its default MINPREFIX.
        private const int MinimumPrefixLength = 2;

        // RediSearch's default stop words are never indexed, so they can only be matched exactly.
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "is", "the", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "it",
            "no", "not", "of", "on", "or", "such", "that", "their", "then", "there", "these", "they", "this", "to",
            "was", "will", "with"
        };

        /// <summary>
        /// Returns the escaped query text for a partial match on the given search.
        /// "Per Tes" becomes "(Per* Tes*)"; short words and stop words stay exact.
        /// </summary>
        /// <param name="search">The text the player or DM typed.</param>
        public static string Build(string search)
        {
            var words = (search ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            // A group of only exact stop words matches every record, so keep the single-prefix form.
            if (!words.Any(CanPrefix))
                return DB.EscapeTokens(string.Join(" ", words)) + "*";

            var terms = words.Select(word => CanPrefix(word)
                ? DB.EscapeTokens(word) + "*"
                : DB.EscapeTokens(word));

            return "(" + string.Join(" ", terms) + ")";
        }

        private static bool CanPrefix(string word)
        {
            return word.Length >= MinimumPrefixLength && !StopWords.Contains(word);
        }
    }
}
