using System.Linq;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorDroidspeak : ITranslator
    {
        public string Translate(string message)
        {
            string[] words = message.Split(' ');

            for (int i = 0; i < words.Length; ++i)
            {
                string word = words[i];

                bool isAllSpecialChars = true;
                string specialCharsAtEndOfWord = "";

                for (int j = 0; j < word.Length; ++j)
                {
                    char ch = word[j];

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
                    bool firstIsUpper = false;

                    foreach (char ch in word)
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
