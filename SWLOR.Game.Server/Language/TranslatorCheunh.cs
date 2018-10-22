using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorCheunh : ITranslator
    {
        public string Translate(string message)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("y"); break;
                    case 'A': sb.Append("Y"); break;

                    case 'b': sb.Append("x"); break;
                    case 'B': sb.Append("X"); break;

                    case 'c': sb.Append("i"); break;
                    case 'C': sb.Append("I"); break;

                    case 'd': sb.Append("c"); break;
                    case 'D': sb.Append("C"); break;

                    case 'e': sb.Append("t"); break;
                    case 'E': sb.Append("T"); break;

                    case 'f': sb.Append("d"); break;
                    case 'F': sb.Append("D"); break;

                    case 'g': sb.Append("o"); break;
                    case 'G': sb.Append("O"); break;

                    case 'h': sb.Append("f"); break;
                    case 'H': sb.Append("F"); break;

                    case 'i': sb.Append("b"); break;
                    case 'I': sb.Append("B"); break;

                    case 'j': sb.Append("i"); break;
                    case 'J': sb.Append("I"); break;

                    case 'k': sb.Append("n"); break;
                    case 'K': sb.Append("N"); break;

                    case 'l': sb.Append("v"); break;
                    case 'L': sb.Append("V"); break;

                    case 'm': sb.Append("l"); break;
                    case 'M': sb.Append("L"); break;

                    case 'n': sb.Append("z"); break;
                    case 'N': sb.Append("Z"); break;

                    case 'o': sb.Append("p"); break;
                    case 'O': sb.Append("P"); break;

                    case 'p': sb.Append("m"); break;
                    case 'P': sb.Append("M"); break;

                    case 'q': sb.Append("h"); break;
                    case 'Q': sb.Append("H"); break;

                    case 'r': sb.Append("k"); break;
                    case 'R': sb.Append("K"); break;

                    case 's': sb.Append("w"); break;
                    case 'S': sb.Append("W"); break;

                    case 't': sb.Append("q"); break;
                    case 'T': sb.Append("Q"); break;

                    case 'u': sb.Append("d"); break;
                    case 'U': sb.Append("D"); break;

                    case 'v': sb.Append("s"); break;
                    case 'V': sb.Append("S"); break;

                    case 'w': sb.Append("e"); break;
                    case 'W': sb.Append("E"); break;

                    case 'x': sb.Append("a"); break;
                    case 'X': sb.Append("A"); break;

                    case 'y': sb.Append("g"); break;
                    case 'Y': sb.Append("G"); break;

                    case 'z': sb.Append("b"); break;
                    case 'Z': sb.Append("B"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
