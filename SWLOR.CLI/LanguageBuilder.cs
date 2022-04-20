using System;
using System.Linq;

namespace SWLOR.CLI
{
    public class LanguageBuilder
    {
        public void Process()
        {
            var rng = new Random();

            var charactersBaseRef = new char[]
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
                'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
            };

            var characterRef = charactersBaseRef.ToList();

            // Add a couple dupes, then remove a couple at random.

            var randomDupes = rng.Next(10);
            var randomRemovals = rng.Next(5);

            for (var i = 0; i < randomDupes; ++i)
            {
                characterRef.Add(charactersBaseRef[rng.Next(charactersBaseRef.Length)]);
            }

            for (var i = 0; i < randomRemovals; ++i)
            {
                characterRef.RemoveAt(rng.Next(characterRef.Count));
            }

            if ((rng.Next(2) % 2) == 0)
            {
                characterRef.Add('\'');
            }

            for (var i = 0; i < charactersBaseRef.Length; ++i)
            {
                var ch = charactersBaseRef[i];

                string substitute;

                if (characterRef.Count != 0)
                {
                    var indexSub = rng.Next(characterRef.Count);
                    substitute = characterRef[indexSub].ToString();
                    characterRef.RemoveAt(indexSub);
                }
                else
                {
                    var indexSub = rng.Next(charactersBaseRef.Length);
                    substitute = charactersBaseRef[indexSub].ToString();
                }

                if (rng.Next(10) == 0 && charactersBaseRef.Length - i < characterRef.Count)
                {
                    var indexSub = rng.Next(characterRef.Count);
                    substitute += characterRef[indexSub].ToString();
                    characterRef.RemoveAt(indexSub);
                }

                var lowercase = $@"case '{ch}': sb.Append(""{substitute}""); break;";
                var uppercase = $@"case '{char.ToUpper(ch)}': sb.Append(""{char.ToUpper(substitute[0])}""); break;";

                Console.WriteLine(lowercase);
                Console.WriteLine(uppercase);
                Console.WriteLine();
            }

            Console.ReadKey();

        }
    }
}
