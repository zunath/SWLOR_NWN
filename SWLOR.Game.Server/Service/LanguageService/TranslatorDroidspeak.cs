using System.Linq;

namespace SWLOR.Game.Server.Service.LanguageService
{
    public class TranslatorDroidspeak : ITranslator
    {
        public string Translate(string message, int englishChance, out string partiallyScrambled)
        {
            partiallyScrambled = "";
            var words = message.Split(' ');

            for (var i = 0; i < words.Length; ++i)
            {
                var word = words[i];

                var isAllSpecialChars = true;
                var specialCharsAtEndOfWord = "";

                for (var j = 0; j < word.Length; ++j)
                {
                    var ch = word[j];

                    if (char.IsLetter(ch))
                    {
                        isAllSpecialChars = false;
                    }
                    else
                    {
                        specialCharsAtEndOfWord += ch;
                    }
                }

                if (!string.IsNullOrEmpty(word) && !isAllSpecialChars)
                {
                    var firstIsUpper = false;

                    foreach (var ch in word)
                    {
                        if (char.IsLetter(ch))
                        {
                            firstIsUpper = char.IsUpper(ch);
                            break;
                        }
                    }

                    if (word.Length <= 2)
                    {
                        word = "do";
                    }
                    else if (word.Length <= 3)
                    {
                        word = "beep";
                    }
                    else if (word.Length <= 4)
                    {
                        word = "boop";
                    }
                    else if (word.Length <= 6)
                    {
                        word = "dreeet";
                    }
                    else if (word.Length <= 8)
                    {
                        word = "blooooo";
                    }
                    else if (word.Length <= 10)
                    {
                        word = "vooooooot";
                    }
                    else if (word.Length <= 12)
                    {
                        word = "bleeeeeeep";
                    }
                    else
                    {
                        word = "breeeeeeeeeeep";
                    }

                    if (firstIsUpper)
                    {
                        word = char.ToUpper(word[0]) + word.Substring(1);
                    }

                    words[i] = word + specialCharsAtEndOfWord;
                }
            }

            return words.Aggregate((a, b) => a + " " + b);
        }
    }
}
