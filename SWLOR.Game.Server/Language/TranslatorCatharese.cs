using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorCatharese : ITranslator
    {
        public string Translate(string message)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("l"); break;
                    case 'A': sb.Append("L"); break;

                    case 'b': sb.Append("a"); break;
                    case 'B': sb.Append("A"); break;

                    case 'c': sb.Append("f"); break;
                    case 'C': sb.Append("F"); break;

                    case 'd': sb.Append("o"); break;
                    case 'D': sb.Append("O"); break;

                    case 'e': sb.Append("z"); break;
                    case 'E': sb.Append("Z"); break;

                    case 'f': sb.Append("z"); break;
                    case 'F': sb.Append("Z"); break;

                    case 'g': sb.Append("g"); break;
                    case 'G': sb.Append("G"); break;

                    case 'h': sb.Append("de"); break;
                    case 'H': sb.Append("D"); break;

                    case 'i': sb.Append("b"); break;
                    case 'I': sb.Append("B"); break;

                    case 'j': sb.Append("n"); break;
                    case 'J': sb.Append("N"); break;

                    case 'k': sb.Append("g"); break;
                    case 'K': sb.Append("G"); break;

                    case 'l': sb.Append("o"); break;
                    case 'L': sb.Append("O"); break;

                    case 'm': sb.Append("y"); break;
                    case 'M': sb.Append("Y"); break;

                    case 'n': sb.Append("q"); break;
                    case 'N': sb.Append("Q"); break;

                    case 'o': sb.Append("kt"); break;
                    case 'O': sb.Append("K"); break;

                    case 'p': sb.Append("h"); break;
                    case 'P': sb.Append("H"); break;

                    case 'q': sb.Append("c"); break;
                    case 'Q': sb.Append("C"); break;

                    case 'r': sb.Append("j"); break;
                    case 'R': sb.Append("J"); break;

                    case 's': sb.Append("q"); break;
                    case 'S': sb.Append("Q"); break;

                    case 't': sb.Append("b"); break;
                    case 'T': sb.Append("B"); break;

                    case 'u': sb.Append("w"); break;
                    case 'U': sb.Append("W"); break;

                    case 'v': sb.Append("i"); break;
                    case 'V': sb.Append("I"); break;

                    case 'w': sb.Append("s"); break;
                    case 'W': sb.Append("S"); break;

                    case 'x': sb.Append("p"); break;
                    case 'X': sb.Append("P"); break;

                    case 'y': sb.Append("e"); break;
                    case 'Y': sb.Append("E"); break;

                    case 'z': sb.Append("'"); break;
                    case 'Z': sb.Append("'"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
