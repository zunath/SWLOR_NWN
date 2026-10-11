using System;
using System.Linq;

namespace SWLOR.Game.Server.Service.DBService
{
    /// <summary>
    /// Builds the RediSearch text for a partial-match field search so every word must match
    /// the start of a word in that field. Relies on the indexes keeping stop words (see SearchIndexDefinition).
    /// </summary>
    public static class PartialMatchSearchText
    {
        // RediSearch rejects prefixes shorter than its default MINPREFIX.
        private const int MinimumPrefixLength = 2;

        /// <summary>
        /// Returns the escaped query text for a partial match on the given search.
        /// "Per Tes" becomes "(Per* Tes*)"; one-character words must match a whole word.
        /// </summary>
        /// <param name="search">The text the player or DM typed.</param>
        public static string Build(string search)
        {
            var words = (search ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return DB.EscapeTokens(search ?? string.Empty) + "*";

            var terms = words.Select(word => word.Length >= MinimumPrefixLength
                ? DB.EscapeTokens(word) + "*"
                : DB.EscapeTokens(word));

            return "(" + string.Join(" ", terms) + ")";
        }
    }
}
