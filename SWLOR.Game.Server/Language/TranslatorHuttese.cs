using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorHuttese : ITranslator
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

                    case 'b': sb.Append("i"); break;
                    case 'B': sb.Append("I"); break;

                    case 'c': sb.Append("v"); break;
                    case 'C': sb.Append("V"); break;

                    case 'd': sb.Append("k"); break;
                    case 'D': sb.Append("K"); break;

                    case 'e': sb.Append("t"); break;
                    case 'E': sb.Append("T"); break;

                    case 'f': sb.Append("y"); break;
                    case 'F': sb.Append("Y"); break;

                    case 'g': sb.Append("j"); break;
                    case 'G': sb.Append("J"); break;

                    case 'h': sb.Append("r"); break;
                    case 'H': sb.Append("R"); break;

                    case 'i': sb.Append("b"); break;
                    case 'I': sb.Append("B"); break;

                    case 'j': sb.Append("l"); break;
                    case 'J': sb.Append("L"); break;

                    case 'k': sb.Append("o"); break;
                    case 'K': sb.Append("O"); break;

                    case 'l': sb.Append("f"); break;
                    case 'L': sb.Append("F"); break;

                    case 'm': sb.Append("w"); break;
                    case 'M': sb.Append("W"); break;

                    case 'n': sb.Append("e"); break;
                    case 'N': sb.Append("E"); break;

                    case 'o': sb.Append("h"); break;
                    case 'O': sb.Append("H"); break;

                    case 'p': sb.Append("g"); break;
                    case 'P': sb.Append("G"); break;

                    case 'q': sb.Append("z"); break;
                    case 'Q': sb.Append("Z"); break;

                    case 'r': sb.Append("'"); break;
                    case 'R': sb.Append("'"); break;

                    case 's': sb.Append("n"); break;
                    case 'S': sb.Append("N"); break;

                    case 't': sb.Append("u"); break;
                    case 'T': sb.Append("U"); break;

                    case 'u': sb.Append("a"); break;
                    case 'U': sb.Append("A"); break;

                    case 'v': sb.Append("m"); break;
                    case 'V': sb.Append("M"); break;

                    case 'w': sb.Append("c"); break;
                    case 'W': sb.Append("C"); break;

                    case 'x': sb.Append("s"); break;
                    case 'X': sb.Append("S"); break;

                    case 'y': sb.Append("g"); break;
                    case 'Y': sb.Append("G"); break;

                    case 'z': sb.Append("p"); break;
                    case 'Z': sb.Append("P"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
