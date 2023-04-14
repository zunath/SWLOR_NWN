using System.Text;

namespace SWLOR.Game.Server.Service.LanguageService
{
    public class TranslatorNautila : ITranslator
    {
        public string Translate(string message)
        {
            var sb = new StringBuilder();

            foreach (var ch in message)
            {
                switch (ch)
                {

                    case 'a': sb.Append("p"); break;
                    case 'A': sb.Append("P"); break;

                    case 'b': sb.Append("x"); break;
                    case 'B': sb.Append("X"); break;

                    case 'c': sb.Append("m"); break;
                    case 'C': sb.Append("M"); break;

                    case 'd': sb.Append("q"); break;
                    case 'D': sb.Append("Q"); break;

                    case 'e': sb.Append("t"); break;
                    case 'E': sb.Append("T"); break;

                    case 'f': sb.Append("l"); break;
                    case 'F': sb.Append("L"); break;

                    case 'g': sb.Append("'"); break;
                    case 'G': sb.Append("'"); break;

                    case 'h': sb.Append("h"); break;
                    case 'H': sb.Append("H"); break;

                    case 'i': sb.Append("dr"); break;
                    case 'I': sb.Append("D"); break;

                    case 'j': sb.Append("b"); break;
                    case 'J': sb.Append("B"); break;

                    case 'k': sb.Append("qn"); break;
                    case 'K': sb.Append("Q"); break;

                    case 'l': sb.Append("o"); break;
                    case 'L': sb.Append("O"); break;

                    case 'm': sb.Append("z"); break;
                    case 'M': sb.Append("Z"); break;

                    case 'n': sb.Append("s"); break;
                    case 'N': sb.Append("S"); break;

                    case 'o': sb.Append("y"); break;
                    case 'O': sb.Append("Y"); break;

                    case 'p': sb.Append("x"); break;
                    case 'P': sb.Append("X"); break;

                    case 'q': sb.Append("a"); break;
                    case 'Q': sb.Append("A"); break;

                    case 'r': sb.Append("u"); break;
                    case 'R': sb.Append("U"); break;

                    case 's': sb.Append("w"); break;
                    case 'S': sb.Append("W"); break;

                    case 't': sb.Append("i"); break;
                    case 'T': sb.Append("I"); break;

                    case 'u': sb.Append("c"); break;
                    case 'U': sb.Append("C"); break;

                    case 'v': sb.Append("g"); break;
                    case 'V': sb.Append("G"); break;

                    case 'w': sb.Append("v"); break;
                    case 'W': sb.Append("V"); break;

                    case 'x': sb.Append("k"); break;
                    case 'X': sb.Append("K"); break;

                    case 'y': sb.Append("f"); break;
                    case 'Y': sb.Append("F"); break;

                    case 'z': sb.Append("e"); break;
                    case 'Z': sb.Append("E"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}