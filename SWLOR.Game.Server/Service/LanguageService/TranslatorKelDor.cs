using System.Text;

namespace SWLOR.Game.Server.Service.LanguageService
{
    public class TranslatorKelDor : ITranslator
    {
        public string Translate(string message, int englishChance, out string partiallyScrambled)
        {
            partiallyScrambled = "";
            var sb = new StringBuilder();

            foreach (var ch in message)
            {
                switch (ch)
                {

                    case 'a': sb.Append("u"); break;
                    case 'A': sb.Append("U"); break;

                    case 'b': sb.Append("w"); break;
                    case 'B': sb.Append("W"); break;

                    case 'c': sb.Append("i"); break;
                    case 'C': sb.Append("I"); break;

                    case 'd': sb.Append("d"); break;
                    case 'D': sb.Append("D"); break;

                    case 'e': sb.Append("l"); break;
                    case 'E': sb.Append("L"); break;

                    case 'f': sb.Append("o"); break;
                    case 'F': sb.Append("O"); break;

                    case 'g': sb.Append("xf"); break;
                    case 'G': sb.Append("X"); break;

                    case 'h': sb.Append("'"); break;
                    case 'H': sb.Append("'"); break;

                    case 'i': sb.Append("z"); break;
                    case 'I': sb.Append("Z"); break;

                    case 'j': sb.Append("t"); break;
                    case 'J': sb.Append("T"); break;

                    case 'k': sb.Append("h"); break;
                    case 'K': sb.Append("H"); break;

                    case 'l': sb.Append("oj"); break;
                    case 'L': sb.Append("O"); break;

                    case 'm': sb.Append("y"); break;
                    case 'M': sb.Append("Y"); break;

                    case 'n': sb.Append("d"); break;
                    case 'N': sb.Append("D"); break;

                    case 'o': sb.Append("n"); break;
                    case 'O': sb.Append("N"); break;

                    case 'p': sb.Append("g"); break;
                    case 'P': sb.Append("G"); break;

                    case 'q': sb.Append("ab"); break;
                    case 'Q': sb.Append("A"); break;

                    case 'r': sb.Append("b"); break;
                    case 'R': sb.Append("B"); break;

                    case 's': sb.Append("p"); break;
                    case 'S': sb.Append("P"); break;

                    case 't': sb.Append("m"); break;
                    case 'T': sb.Append("M"); break;

                    case 'u': sb.Append("s"); break;
                    case 'U': sb.Append("S"); break;

                    case 'v': sb.Append("k"); break;
                    case 'V': sb.Append("K"); break;

                    case 'w': sb.Append("we"); break;
                    case 'W': sb.Append("W"); break;

                    case 'x': sb.Append("g"); break;
                    case 'X': sb.Append("G"); break;

                    case 'y': sb.Append("v"); break;
                    case 'Y': sb.Append("V"); break;

                    case 'z': sb.Append("q"); break;
                    case 'Z': sb.Append("Q"); break;
                }
            }

            return sb.ToString();
        }
    }
}
