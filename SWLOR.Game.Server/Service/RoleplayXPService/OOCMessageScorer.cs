using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.RoleplayXPService
{
    /// <summary>
    /// Scores how likely a message is OOC chatter using feature weights from the external detection file.
    /// Feature extraction must stay identical to tools/TrainRoleplayOOCModel.py, which produces the weights.
    /// </summary>
    public static class OOCMessageScorer
    {
        private static readonly string[] Markers = { "//", @"\\", "((", "))", "[[", "]]", "{{", "}}" };
        private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Compiled;

        private static readonly Regex OOCLabel = new(@"^\s*(\[ooc\]|\(ooc\)|ooc:)\s*", Options | RegexOptions.IgnoreCase);
        private static readonly Regex Word = new("[a-z0-9']+", Options);
        private static readonly Regex Emoticon = new(
            @"(?<!\S)(?:[>]?[:;=][-']?[)(dpo3c/|\]\[]+|<3|\^\^|\^_\^|o7|t_t|;_;|;-;)(?!\S)",
            Options | RegexOptions.IgnoreCase);
        private static readonly Regex Elongated = new(@"([a-z])\1\1", Options | RegexOptions.IgnoreCase);
        private static readonly Regex SpacedLetters = new(@"(?:\b\w\b\s){3,}\b\w\b", Options);
        private static readonly Regex LowercasePronoun = new(@"(?<![\w'])(i|im|ive|ill|id|u|ur)(?![\w'])", Options);

        public static double Score(string message, EmoteStyle emoteStyle, IReadOnlyDictionary<string, double> weights)
        {
            var total = 0.0;
            foreach (var feature in ExtractFeatures(message, emoteStyle))
            {
                if (weights.TryGetValue(feature, out var weight))
                    total += weight;
            }

            return 1.0 / (1.0 + Math.Exp(-Math.Clamp(total, -30.0, 30.0)));
        }

        public static HashSet<string> ExtractFeatures(string message, EmoteStyle emoteStyle)
        {
            var features = new HashSet<string>(StringComparer.Ordinal) { "bias" };
            if (message.Contains('"'))
                features.Add("has_quote");

            var text = RemoveQuotedSpeech(StripMarkers(message)).Trim();
            if (text.Length == 0)
            {
                features.Add("empty_narration");
                return features;
            }

            var words = Word.Matches(text.ToLowerInvariant()).Select(match => match.Value).ToList();
            features.Add("len:" + (words.Count <= 1 ? "1" :
                words.Count <= 3 ? "2-3" :
                words.Count <= 7 ? "4-7" :
                words.Count <= 15 ? "8-15" : "16+"));

            var letters = text.Where(char.IsLetter).ToList();
            if (letters.Count > 0)
            {
                if (letters.All(char.IsLower))
                    features.Add("all_lower");
                else if (letters.All(char.IsUpper) && letters.Count >= 3)
                    features.Add("all_caps");

                if (char.IsLower(letters[0]))
                    features.Add("lower_start");
            }

            if (".!?*-~\"'".IndexOf(text[^1]) < 0)
                features.Add("no_end_punct");
            if (text.Contains('?'))
                features.Add("question");
            if (text.Contains('*'))
                features.Add("asterisk");
            if (text.Any(char.IsDigit))
                features.Add("digits");
            if (Elongated.IsMatch(text))
                features.Add("elongated");
            if (SpacedLetters.IsMatch(text))
                features.Add("spaced_letters");
            if (Emoticon.IsMatch(text))
                features.Add("emoticon");
            if (text.Contains('^'))
                features.Add("caret");

            foreach (Match match in LowercasePronoun.Matches(text))
            {
                features.Add("lc:" + match.Groups[1].Value);
            }

            var style = emoteStyle == EmoteStyle.Novel ? "n:" : "r:";
            foreach (var word in words)
            {
                features.Add("w:" + word);
                features.Add(style + word);
            }

            for (var index = 0; index + 1 < words.Count; index++)
            {
                features.Add("b:" + words[index] + "_" + words[index + 1]);
            }

            return features;
        }

        private static string StripMarkers(string message)
        {
            var text = OOCLabel.Replace(message, string.Empty, 1);
            foreach (var marker in Markers)
            {
                text = text.Replace(marker, " ", StringComparison.Ordinal);
            }

            return text;
        }

        private static string RemoveQuotedSpeech(string text)
        {
            var characters = text.ToCharArray();
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
    }
}
