using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorDosh : ITranslator
    {
        public string Translate(string message)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("x"); break;
                    case 'A': sb.Append("X"); break;

                    case 'b': sb.Append("s"); break;
                    case 'B': sb.Append("S"); break;

                    case 'c': sb.Append("t"); break;
                    case 'C': sb.Append("T"); break;

                    case 'd': sb.Append("g"); break;
                    case 'D': sb.Append("G"); break;

                    case 'e': sb.Append("a"); break;
                    case 'E': sb.Append("A"); break;

                    case 'f': sb.Append("'"); break;
                    case 'F': sb.Append("'"); break;

                    case 'g': sb.Append("h"); break;
                    case 'G': sb.Append("H"); break;

                    case 'h': sb.Append("b"); break;
                    case 'H': sb.Append("B"); break;

                    case 'i': sb.Append("z"); break;
                    case 'I': sb.Append("Z"); break;

                    case 'j': sb.Append("k"); break;
                    case 'J': sb.Append("K"); break;

                    case 'k': sb.Append("c"); break;
                    case 'K': sb.Append("C"); break;

                    case 'l': sb.Append("y"); break;
                    case 'L': sb.Append("Y"); break;

                    case 'm': sb.Append("l"); break;
                    case 'M': sb.Append("L"); break;

                    case 'n': sb.Append("n"); break;
                    case 'N': sb.Append("N"); break;

                    case 'o': sb.Append("p"); break;
                    case 'O': sb.Append("P"); break;

                    case 'p': sb.Append("q"); break;
                    case 'P': sb.Append("Q"); break;

                    case 'q': sb.Append("e"); break;
                    case 'Q': sb.Append("E"); break;

                    case 'r': sb.Append("r"); break;
                    case 'R': sb.Append("R"); break;

                    case 's': sb.Append("o"); break;
                    case 'S': sb.Append("O"); break;

                    case 't': sb.Append("v"); break;
                    case 'T': sb.Append("V"); break;

                    case 'u': sb.Append("u"); break;
                    case 'U': sb.Append("U"); break;

                    case 'v': sb.Append("f"); break;
                    case 'V': sb.Append("F"); break;

                    case 'w': sb.Append("j"); break;
                    case 'W': sb.Append("J"); break;

                    case 'x': sb.Append("w"); break;
                    case 'X': sb.Append("W"); break;

                    case 'y': sb.Append("e"); break;
                    case 'Y': sb.Append("E"); break;

                    case 'z': sb.Append("u"); break;
                    case 'Z': sb.Append("U"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
