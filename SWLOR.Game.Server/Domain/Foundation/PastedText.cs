using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Cleans player-written text pasted from documents and chat apps so the game shows it without stray boxes:
    /// Windows line endings, typographic punctuation, special spaces, and invisible formatting characters.
    /// </summary>
    public static class PastedText
    {
        private const int ColorStartTokenLength = 6;

        // Code points rather than literals, so the game-text encoding test keeps this file free of typographic punctuation.
        private static readonly FrozenDictionary<int, string> Replacements = new Dictionary<int, string>
        {
            // Single quotes and apostrophes.
            [0x2018] = "'", [0x2019] = "'", [0x201A] = "'", [0x201B] = "'", [0x2032] = "'", [0x02BC] = "'",
            [0x2039] = "'", [0x203A] = "'",
            // Double quotes.
            [0x201C] = "\"", [0x201D] = "\"", [0x201E] = "\"", [0x201F] = "\"", [0x2033] = "\"",
            // Hyphens, dashes, and minus.
            [0x2010] = "-", [0x2011] = "-", [0x2012] = "-", [0x2013] = "-", [0x2014] = "-", [0x2015] = "-",
            [0x2212] = "-",
            // Ellipsis and list bullets.
            [0x2026] = "...", [0x2022] = "-", [0x25CF] = "-", [0x25E6] = "-", [0x25AA] = "-", [0x25A0] = "-",
            // Line and paragraph separators.
            [0x0085] = "\n", [0x2028] = "\n", [0x2029] = "\n", ['\v'] = "\n", ['\f'] = "\n",
            ['\t'] = "    ",
        }.ToFrozenDictionary();

        // The characters Windows-1252 places in 0x80-0x9F; with Latin-1 they make up the game's character set.
        private static readonly FrozenSet<int> Windows1252HighCharacters = new HashSet<int>
        {
            0x20AC, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021, 0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x017D,
            0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014, 0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x017E,
            0x0178,
        }.ToFrozenSet();

        /// <summary>
        /// Returns the text with line endings changed to '\n', typographic punctuation changed to ASCII, special
        /// spaces changed to plain spaces, invisible formatting characters removed, and anything the game cannot
        /// show changed to '?'. Accented letters are kept.
        /// </summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var result = new StringBuilder(text.Length);
            var index = 0;
            while (index < text.Length)
            {
                // Color token channels are raw bytes and may be control characters, so tokens pass through whole.
                if (IsColorStartToken(text, index))
                {
                    result.Append(text, index, ColorStartTokenLength);
                    index += ColorStartTokenLength;
                    continue;
                }

                Rune.DecodeFromUtf16(text.AsSpan(index), out var rune, out var consumed);
                index += consumed;
                var value = rune.Value;
                if (Replacements.TryGetValue(value, out var replacement))
                    result.Append(replacement);
                else if (value == '\n')
                    result.Append('\n');
                else if (Rune.IsWhiteSpace(rune))
                    result.Append(' ');
                else if (IsInvisible(rune))
                    continue;
                else if (value <= 0xFF || Windows1252HighCharacters.Contains(value))
                    result.Append(rune.ToString());
                else
                    result.Append('?');
            }

            return result.ToString();
        }

        private static bool IsColorStartToken(string text, int index)
        {
            return index + ColorStartTokenLength <= text.Length &&
                   text[index] == '<' && text[index + 1] == 'c' && text[index + 5] == '>';
        }

        private static bool IsInvisible(Rune rune)
        {
            var category = Rune.GetUnicodeCategory(rune);
            return category is UnicodeCategory.Control or UnicodeCategory.Format;
        }
    }
}
