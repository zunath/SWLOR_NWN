using System.Linq;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorShyriiwook : ITranslator
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
                        word = "huurh";
                    }
                    else if (word.Length <= 3)
                    {
                        word = "wrrhwh";
                    }
                    else if (word.Length <= 4)
                    {
                        word = "aaahnruh";
                    }
                    else if (word.Length <= 5)
                    {
                        word = "uggguh";
                    }
                    else if (word.Length <= 6)
                    {
                        word = "aaaaahnr";
                    }
                    else if (word.Length <= 7)
                    {
                        word = "hnnrrhhh";
                    }
                    else if (word.Length <= 8)
                    {
                        word = "wrrhwrwwhw";
                    }
                    else if (word.Length <= 10)
                    {
                        word = "raaaaaahhgh";
                    }
                    else if (word.Length <= 12)
                    {
                        word = "aarrragghuuhw";
                    }
                    else
                    {
                        word = "uughguughhhghghghhhgh";
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
