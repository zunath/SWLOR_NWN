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

        /// <summary>
        /// Returns the escaped query text for a partial match on the given search.
        /// "Per Tes" becomes "(Per* Tes*)"; one-character words must match a whole word.
        /// </summary>
        /// <param name="search">The text the player or DM typed.</param>
        public static string Build(string search)
        {
            search ??= string.Empty;

            // Stored values pass through DB.EscapeTokens before indexing, so split the search the same way.
            var terms = SplitWords(DB.EscapeTokens(search))
                .Select(BuildTerm)
                .Where(term => term != null)
                .ToList();

            if (terms.Count == 0)
            {
                // Nothing searchable: escape every character so the query stays valid.
                var escaped = string.Concat(search.Where(c => c != ' ' && c != '\t').Select(c => "\\" + c));
                return "(" + escaped + ")";
            }

            return "(" + string.Join(" ", terms) + ")";
        }

        /// <summary>
        /// Splits text into words the way the index splits a stored value, so searches over records that are
        /// not in the index (such as online players) can match the same way as <see cref="Build"/>.
        /// </summary>
        /// <param name="text">A stored value or the text the player or DM typed.</param>
        public static IReadOnlyList<string> GetWords(string text)
        {
            return SplitWords(DB.EscapeTokens(text ?? string.Empty));
        }

        /// <summary>
        /// Returns whether a search word from <see cref="GetWords"/> matches a stored word the way the query from
        /// <see cref="Build"/> does: the start of the word, or the whole word when the search word is one character.
        /// </summary>
        /// <param name="searchWord">One word of the search.</param>
        /// <param name="storedWord">One word of a stored value.</param>
        public static bool MatchesWord(string searchWord, string storedWord)
        {
            var backslash = FirstUnqueryableBackslash(searchWord);
            if (backslash >= 0)
            {
                // Build matches only up to the backslash, and drops the word when that part is too short.
                var prefix = searchWord[..(backslash + 1)];
                return !CanPrefix(prefix) || storedWord.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return CanPrefix(searchWord)
                ? storedWord.StartsWith(searchWord, StringComparison.OrdinalIgnoreCase)
                : string.Equals(storedWord, searchWord, StringComparison.OrdinalIgnoreCase);
        }

        // Mirrors the index tokenizer: a separator right after a backslash does not split, and inside a word
        // an unpaired backslash makes the next character literal while control characters are dropped.
        private static List<string> SplitWords(string indexedText)
        {
            var words = new List<string>();
            var current = new StringBuilder();
            var escaped = false;

            for (var index = 0; index < indexedText.Length; index++)
            {
                var character = indexedText[index];
                var afterBackslash = index > 0 && indexedText[index - 1] == '\\';

                if (IndexSeparators.IndexOf(character) >= 0 && !afterBackslash)
                {
                    if (current.Length > 0)
                    {
                        words.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                if (character < 32 || character == 127 || (character == ' ' && !escaped))
                {
                    escaped = false;
                    continue;
                }

                if (character == '\\' && !escaped)
                {
                    escaped = true;
                    continue;
                }

                current.Append(character);
                escaped = false;
            }

            if (current.Length > 0)
                words.Add(current.ToString());

            return words;
        }

        private static string BuildTerm(string word)
        {
            var backslash = FirstUnqueryableBackslash(word);
            if (backslash < 0)
                return CanPrefix(word) ? EscapeWord(word) + "*" : EscapeWord(word);

            // A query cannot spell a backslash followed by punctuation, so match the word up to the backslash.
            var prefix = word[..(backslash + 1)];
            return CanPrefix(prefix) ? EscapeWord(prefix) + "*" : null;
        }

        // Queries only match a literal backslash that ends the word or precedes a letter, digit or non-ASCII character.
        private static int FirstUnqueryableBackslash(string word)
        {
            for (var index = 0; index + 1 < word.Length; index++)
            {
                if (word[index] == '\\' && IsAsciiNonAlphanumeric(word[index + 1]))
                    return index;
            }

            return -1;
        }

        // Punctuation inside a word must be escaped in the query or RediSearch treats it as syntax.
        private static string EscapeWord(string word)
        {
            var builder = new StringBuilder(word.Length * 2);
            foreach (var character in word)
            {
                if (IsAsciiNonAlphanumeric(character) && character != '_')
                    builder.Append('\\');
                builder.Append(character);
            }

            return builder.ToString();
        }

        private static bool IsAsciiNonAlphanumeric(char character)
        {
            return character < 128 && !char.IsLetterOrDigit(character);
        }

        // RediSearch prefix queries need two characters and do not match characters beyond U+FFFF.
        private static bool CanPrefix(string word)
        {
            var runes = word.EnumerateRunes().ToList();
            return runes.Count >= MinimumPrefixLength && runes.All(rune => rune.Value <= 0xFFFF);
        }
    }
}
