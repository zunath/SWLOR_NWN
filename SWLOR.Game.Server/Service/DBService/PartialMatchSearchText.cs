using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        // DB.EscapeTokens escapes these, so they stay part of a word. Any other punctuation separates
        // words, as it does in the index, and is a syntax error when it ends a "word*" term.
        private const string EscapedCharacters = "@!{}()|-=>'\"";

        /// <summary>
        /// Returns the escaped query text for a partial match on the given search.
        /// "Per Tes" becomes "(Per* Tes*)"; one-character words must match a whole word.
        /// </summary>
        /// <param name="search">The text the player or DM typed.</param>
        public static string Build(string search)
        {
            var words = SplitWords(search ?? string.Empty);
            if (words.Count == 0)
            {
                // Only punctuation: escape every character so the query stays valid.
                var escaped = string.Concat((search ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).Select(c => "\\" + c));
                return "(" + escaped + ")";
            }

            var terms = words.Select(word => word.Length >= MinimumPrefixLength
                ? DB.EscapeTokens(word) + "*"
                : DB.EscapeTokens(word));

            return "(" + string.Join(" ", terms) + ")";
        }

        private static List<string> SplitWords(string search)
        {
            var words = new List<string>();
            var current = new StringBuilder();

            foreach (var character in search)
            {
                if (char.IsLetterOrDigit(character) || character == '_' || EscapedCharacters.IndexOf(character) >= 0)
                {
                    current.Append(character);
                    continue;
                }

                if (current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }
            }

            if (current.Length > 0)
                words.Add(current.ToString());

            return words;
        }
    }
}
