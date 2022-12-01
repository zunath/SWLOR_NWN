using System.Text;

namespace SWLOR.Game.Server.Service.LanguageService
{
    public class TranslatorTogruti : ITranslator
    {
        public string Translate(string message, int englishChance, out string partiallyScrambled)
        {
            partiallyScrambled = "";
            var sb = new StringBuilder();

            foreach (var ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("k"); break;
                    case 'A': sb.Append("K"); break;

                    case 'b': sb.Append("r"); break;
                    case 'B': sb.Append("R"); break;

                    case 'c': sb.Append("a"); break;
                    case 'C': sb.Append("A"); break;

                    case 'd': sb.Append("n"); break;
                    case 'D': sb.Append("N"); break;

                    case 'e': sb.Append("g"); break;
                    case 'E': sb.Append("G"); break;

                    case 'f': sb.Append("i"); break;
                    case 'F': sb.Append("I"); break;

                    case 'g': sb.Append("'"); break;
                    case 'G': sb.Append("'"); break;

                    case 'h': sb.Append("w"); break;
                    case 'H': sb.Append("W"); break;

                    case 'i': sb.Append("p"); break;
                    case 'I': sb.Append("P"); break;

                    case 'j': sb.Append("xj"); break;
                    case 'J': sb.Append("X"); break;

                    case 'k': sb.Append("q"); break;
                    case 'K': sb.Append("Q"); break;

                    case 'l': sb.Append("ds"); break;
                    case 'L': sb.Append("D"); break;

                    case 'm': sb.Append("f"); break;
                    case 'M': sb.Append("F"); break;

                    case 'n': sb.Append("u"); break;
                    case 'N': sb.Append("U"); break;

                    case 'o': sb.Append("q"); break;
                    case 'O': sb.Append("Q"); break;

                    case 'p': sb.Append("t"); break;
                    case 'P': sb.Append("T"); break;

                    case 'q': sb.Append("e"); break;
                    case 'Q': sb.Append("E"); break;

                    case 'r': sb.Append("z"); break;
                    case 'R': sb.Append("Z"); break;

                    case 's': sb.Append("z"); break;
                    case 'S': sb.Append("Z"); break;

                    case 't': sb.Append("g"); break;
                    case 'T': sb.Append("G"); break;

                    case 'u': sb.Append("l"); break;
                    case 'U': sb.Append("L"); break;

                    case 'v': sb.Append("c"); break;
                    case 'V': sb.Append("C"); break;

                    case 'w': sb.Append("o"); break;
                    case 'W': sb.Append("O"); break;

                    case 'x': sb.Append("v"); break;
                    case 'X': sb.Append("V"); break;

                    case 'y': sb.Append("m"); break;
                    case 'Y': sb.Append("M"); break;

                    case 'z': sb.Append("ys"); break;
                    case 'Z': sb.Append("Y"); break;
                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}