using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Service.RoleplayXPService
{
    /// <summary>
    /// Classifies chat for RP XP. Quoted speech is always treated as in-character; everything outside
    /// quotes is checked for OOC markers, so OOC chatter typed as novel-style emote text is still caught.
    /// </summary>
    public static class RoleplayMessageClassifier
    {
        private static readonly string[] OOCMarkers = { "((", "))", "[[", "]]", "{{", "}}", @"\\" };

        /// <summary>
        /// Words and emoticons treated as OOC outside quoted speech when swlor.env does not
        /// configure its own list.
        /// </summary>
        public static readonly IReadOnlySet<string> DefaultOOCTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Chat shorthand
            "afaik", "afk", "atm", "bbiab", "bbl", "brb", "btw", "g2g", "gg", "gtg", "idc", "idk", "iirc", "ikr",
            "imho", "imo", "irl", "jk", "lmao", "lmfao", "lol", "lul", "ngl", "np", "nvm", "ofc", "omfg", "omg",
            "ooc", "pls", "plz", "rofl", "rq", "smh", "tbh", "thx", "ty", "tyvm", "wtf", "xd",

            // Game and player references
            "dm", "dms", "emote", "emotes", "emoting", "hotbar", "keybind", "keybinds", "npc", "npcs", "relog",
            "relogged", "relogging", "rp", "rping", "screenie", "screenies", "screenshot", "screenshots",
            "toolset", "toon", "toons", "typo", "typos", "xp",

            // Emoticons
            ":)", ":(", ":d", ":p", ":3", ":o", ":c", ":/", ":|", ";)", ";(", ";p", ";d", "=)", "=(", "=d", "=p",
            ":-)", ":-(", ":-d", ":-p", ";-)", ":')", ":'(", ">:)", ">:(", ">:3", ">:d", ":3c", ">:3c",
            "^^", "^_^", "<3", "</3", "o7", "t_t", ";_;", ";-;"
        };

        // Stretched laughter such as "lolol", "lmaooo", and "xDD" is reduced to its base term so it
        // only matches when that base term is in the configured list.
        private static readonly Regex StretchedLol = new(
            "^l(o+l)+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex StretchedLmao = new(
            "^lm(f?)a+o+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex StretchedXD = new(
            "^x+d+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly char[] WordPunctuation = "*.,!?;:()[]{}<>~_-/\\'@#".ToCharArray();
        private static readonly char[] EmoticonPunctuation = "*.,!?".ToCharArray();

        public static RoleplayMessageType Classify(string message, IReadOnlySet<string> oocTerms)
        {
            var trimmed = message?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
                return RoleplayMessageType.InCharacter;

            // Commands such as "/emote" are never shown as chat, so their wording is not OOC chatter.
            if (trimmed.Length >= 2 && trimmed[0] == '/' && trimmed[1] != '/')
                return RoleplayMessageType.ChatCommand;

            var narration = RemoveQuotedSpeech(trimmed);
            if (ContainsOOCMarker(narration) || ContainsOOCTerm(narration, oocTerms))
                return RoleplayMessageType.OutOfCharacter;

            if (IsTypoCorrection(trimmed))
                return RoleplayMessageType.TypoCorrection;

            return RoleplayMessageType.InCharacter;
        }

        /// <summary>
        /// Blanks out double-quoted speech. An unclosed quote runs to the end of the message,
        /// matching how novel-style chat colors it.
        /// </summary>
        private static string RemoveQuotedSpeech(string message)
        {
            var characters = message.ToCharArray();
            var isQuoted = false;
            for (var index = 0; index < characters.Length; index++)
            {
                if (characters[index] == '"')
                {
                    isQuoted = !isQuoted;
                    characters[index] = ' ';
                }
                else if (isQuoted)
                {
                    characters[index] = ' ';
                }
            }

            return new string(characters);
        }

        private static bool ContainsOOCMarker(string narration)
        {
            foreach (var marker in OOCMarkers)
            {
                if (narration.Contains(marker, StringComparison.Ordinal))
                    return true;
            }

            // "//" outside a URL scheme such as "https://".
            for (var index = narration.IndexOf("//", StringComparison.Ordinal);
                 index >= 0;
                 index = narration.IndexOf("//", index + 2, StringComparison.Ordinal))
            {
                if (index == 0 || narration[index - 1] != ':')
                    return true;
            }

            return false;
        }

        private static bool ContainsOOCTerm(string narration, IReadOnlySet<string> oocTerms)
        {
            foreach (var token in narration.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                // Emoticons keep their own punctuation, so they are matched before words are trimmed.
                var emoticonToken = token.Trim(EmoticonPunctuation);
                if (oocTerms.Contains(emoticonToken) || EndsWithAttachedEmoticon(emoticonToken, oocTerms))
                    return true;

                var word = token.Trim(WordPunctuation);
                if (word.Length == 0)
                    continue;

                if (oocTerms.Contains(word) || oocTerms.Contains(ReduceStretchedLaughter(word)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Catches an emoticon typed directly after a word, such as "thanks:)". The word must be at
        /// least two letters so ratios and scores like "K:D" or "5:3" are not mistaken for emoticons.
        /// </summary>
        private static bool EndsWithAttachedEmoticon(string token, IReadOnlySet<string> oocTerms)
        {
            foreach (var term in oocTerms)
            {
                // Only punctuation-led terms are emoticons; word terms must stand alone.
                if (term.Length == 0 || char.IsLetterOrDigit(term[0]))
                    continue;

                var wordLength = token.Length - term.Length;
                if (wordLength < 2 || !token.EndsWith(term, StringComparison.OrdinalIgnoreCase))
                    continue;

                var isWord = true;
                for (var index = 0; index < wordLength && isWord; index++)
                {
                    isWord = char.IsLetter(token[index]);
                }

                if (isWord)
                    return true;
            }

            return false;
        }

        private static string ReduceStretchedLaughter(string word)
        {
            if (StretchedLol.IsMatch(word))
                return "lol";

            var lmao = StretchedLmao.Match(word);
            if (lmao.Success)
                return lmao.Groups[1].Length > 0 ? "lmfao" : "lmao";

            return StretchedXD.IsMatch(word) ? "xd" : word;
        }

        /// <summary>
        /// A short follow-up such as "their*" that fixes a typo in the previous message. This is checked
        /// after OOC detection so appending "*" to OOC chatter such as "brb*" cannot hide it.
        /// </summary>
        private static bool IsTypoCorrection(string message)
        {
            if (!message.EndsWith('*') ||
                message.IndexOf('*') != message.Length - 1 ||
                message.Contains('"'))
                return false;

            var words = message.TrimEnd('*').Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return words.Length is > 0 and <= 3;
        }
    }
}
