using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Tools.LanguageGenerator
{
    public class LanguageGenerator
    {
        static void Main(string[] args)
        {
            Random rng = new Random();

            char[] charactersBaseRef = new char[]
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
                'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
            };

            List<char> characterRef = charactersBaseRef.ToList();

            // Add a couple dupes, then remove a couple at random.

            int randomDupes = rng.Next(10);
            int randomRemovals = rng.Next(5);

            for (int i = 0; i < randomDupes; ++i)
            {
                characterRef.Add(charactersBaseRef[rng.Next(charactersBaseRef.Length)]);
            }

            for (int i = 0; i < randomRemovals; ++i)
            {
                characterRef.RemoveAt(rng.Next(characterRef.Count));
            }

            if ((rng.Next(2) % 2) == 0)
            {
                characterRef.Add('\'');
            }

            for (int i = 0; i < charactersBaseRef.Length; ++i)
            {
                char ch = charactersBaseRef[i];

                string substitute;

                if (characterRef.Count != 0)
                {
                    int indexSub = rng.Next(characterRef.Count);
                    substitute = characterRef[indexSub].ToString();
                    characterRef.RemoveAt(indexSub);
                }
                else
                {
                    int indexSub = rng.Next(charactersBaseRef.Length);
                    substitute = charactersBaseRef[indexSub].ToString();
                }

                if (rng.Next(10) == 0 && charactersBaseRef.Length - i < characterRef.Count)
                {
                    int indexSub = rng.Next(characterRef.Count);
                    substitute += characterRef[indexSub].ToString();
                    characterRef.RemoveAt(indexSub);
                }

                string lowercase = $@"case '{ch}': sb.Append(""{substitute}""); break;";
                string uppercase = $@"case '{char.ToUpper(ch)}': sb.Append(""{char.ToUpper(substitute[0])}""); break;";

                System.Console.WriteLine(lowercase);
                System.Console.WriteLine(uppercase);
                System.Console.WriteLine();
            }

            System.Console.ReadKey();
        }
    }
}