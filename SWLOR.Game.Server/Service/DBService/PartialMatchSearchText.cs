using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWLOR.Game.Server.Service.DBService
{
    /// <summary>
    /// Builds the RediSearch text for a partial-match field search so every word must match
    /// the start of a word in that field. Relies on the indexes keeping stop words (see SearchIndexDefinition).
    /// Words are split exactly as the RediSearch tokenizer splits stored values.
    /// </summary>
    public static class PartialMatchSearchText
    {
        // RediSearch rejects prefixes shorter than its default MINPREFIX.
        private const int MinimumPrefixLength = 2;

        // Characters the index tokenizer splits on (verified against redislabs/redismod). Other whitespace,
        // underscores, backslashes and non-ASCII characters stay inside a word.
        private const string IndexSeparators = " \t,./<>[]:;~#$%^&*+`?!@{}()|-='\"";

        // DB.EscapeTokens escapes these in stored values, so they are part of indexed words, not separators.
        private const string EscapedInStoredValues = "@!{}()|-=>'\"";

        /// <summary>
        /// Returns the escaped query text for a partial match on the given search.
        /// "Per Tes" becomes "(Per* Tes*)"; one-character words must match a whole word.
        /// </summary>
        /// <param name="search">The text the player or DM typed.</param>
        public static string Build(string search)
        {
            search ??= string.Empty;
            var words = SplitWords(search);
            if (words.Count == 0)
            {
                // Only separators: escape every character so the query stays valid.
                var escaped = string.Concat(search.Where(c => c != ' ' && c != '\t').Select(c => "\\" + c));
                return "(" + escaped + ")";
            }

            var terms = words.Select(word => CanPrefix(word) ? EscapeWord(word) + "*" : EscapeWord(word));
            return "(" + string.Join(" ", terms) + ")";
        }

        private static List<string> SplitWords(string search)
        {
            var words = new List<string>();
            var current = new StringBuilder();

            for (var index = 0; index < search.Length; index++)
            {
                var character = search[index];

                // As in the index, a backslash makes the next character literal and is not part of the word.
                if (character == '\\')
                {
                    if (index + 1 < search.Length)
                        current.Append(search[++index]);
                    continue;
                }

                if (IndexSeparators.IndexOf(character) >= 0 && EscapedInStoredValues.IndexOf(character) < 0)
                {
                    if (current.Length > 0)
                    {
                        words.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(character);
            }

            if (current.Length > 0)
                words.Add(current.ToString());

            return words;
        }

        // Punctuation inside a word must be escaped in the query or RediSearch treats it as syntax.
        private static string EscapeWord(string word)
        {
            var builder = new StringBuilder(word.Length * 2);
            foreach (var character in word)
            {
                if (character < 128 && !char.IsLetterOrDigit(character) && character != '_')
                    builder.Append('\\');
                builder.Append(character);
            }

            return builder.ToString();
        }

        // RediSearch prefix queries need two characters and do not match characters beyond U+FFFF.
        private static bool CanPrefix(string word)
        {
            var runes = word.EnumerateRunes().ToList();
            return runes.Count >= MinimumPrefixLength && runes.All(rune => rune.Value <= 0xFFFF);
        }
    }
}
