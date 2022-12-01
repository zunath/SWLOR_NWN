using System.Text;

namespace SWLOR.Game.Server.Service.LanguageService
{
    public class TranslatorRodese : ITranslator
    {
        public string Translate(string message, int englishChance, out string partiallyScrambled)
        {
            partiallyScrambled = "";
            var sb = new StringBuilder();

            foreach (var ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("'"); break;
                    case 'A': sb.Append("'"); break;

                    case 'b': sb.Append("n"); break;
                    case 'B': sb.Append("N"); break;

                    case 'c': sb.Append("z"); break;
                    case 'C': sb.Append("Z"); break;

                    case 'd': sb.Append("cu"); break;
                    case 'D': sb.Append("C"); break;

                    case 'e': sb.Append("p"); break;
                    case 'E': sb.Append("P"); break;

                    case 'f': sb.Append("z"); break;
                    case 'F': sb.Append("Z"); break;

                    case 'g': sb.Append("e"); break;
                    case 'G': sb.Append("E"); break;

                    case 'h': sb.Append("v"); break;
                    case 'H': sb.Append("V"); break;

                    case 'i': sb.Append("fv"); break;
                    case 'I': sb.Append("F"); break;

                    case 'j': sb.Append("l"); break;
                    case 'J': sb.Append("L"); break;

                    case 'k': sb.Append("u"); break;
                    case 'K': sb.Append("U"); break;

                    case 'l': sb.Append("o"); break;
                    case 'L': sb.Append("O"); break;

                    case 'm': sb.Append("q"); break;
                    case 'M': sb.Append("Q"); break;

                    case 'n': sb.Append("t"); break;
                    case 'N': sb.Append("T"); break;

                    case 'o': sb.Append("g"); break;
                    case 'O': sb.Append("G"); break;

                    case 'p': sb.Append("a"); break;
                    case 'P': sb.Append("A"); break;

                    case 'q': sb.Append("w"); break;
                    case 'Q': sb.Append("W"); break;

                    case 'r': sb.Append("y"); break;
                    case 'R': sb.Append("Y"); break;

                    case 's': sb.Append("d"); break;
                    case 'S': sb.Append("D"); break;

                    case 't': sb.Append("a"); break;
                    case 'T': sb.Append("A"); break;

                    case 'u': sb.Append("i"); break;
                    case 'U': sb.Append("I"); break;

                    case 'v': sb.Append("m"); break;
                    case 'V': sb.Append("M"); break;

                    case 'w': sb.Append("k"); break;
                    case 'W': sb.Append("K"); break;

                    case 'x': sb.Append("g"); break;
                    case 'X': sb.Append("G"); break;

                    case 'y': sb.Append("j"); break;
                    case 'Y': sb.Append("J"); break;

                    case 'z': sb.Append("m"); break;
                    case 'Z': sb.Append("M"); break;
                }
            }

            return sb.ToString();
        }
    }
}