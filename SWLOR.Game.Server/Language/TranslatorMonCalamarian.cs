using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorMonCalamarian : ITranslator
    {
        public string Translate(string message)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("w"); break;
                    case 'A': sb.Append("W"); break;

                    case 'b': sb.Append("i"); break;
                    case 'B': sb.Append("I"); break;

                    case 'c': sb.Append("v"); break;
                    case 'C': sb.Append("V"); break;

                    case 'd': sb.Append("h"); break;
                    case 'D': sb.Append("H"); break;

                    case 'e': sb.Append("b"); break;
                    case 'E': sb.Append("B"); break;

                    case 'f': sb.Append("n"); break;
                    case 'F': sb.Append("N"); break;

                    case 'g': sb.Append("q"); break;
                    case 'G': sb.Append("Q"); break;

                    case 'h': sb.Append("'"); break;
                    case 'H': sb.Append("'"); break;

                    case 'i': sb.Append("i"); break;
                    case 'I': sb.Append("I"); break;

                    case 'j': sb.Append("a"); break;
                    case 'J': sb.Append("A"); break;

                    case 'k': sb.Append("c"); break;
                    case 'K': sb.Append("C"); break;

                    case 'l': sb.Append("n"); break;
                    case 'L': sb.Append("N"); break;

                    case 'm': sb.Append("xt"); break;
                    case 'M': sb.Append("X"); break;

                    case 'n': sb.Append("q"); break;
                    case 'N': sb.Append("Q"); break;

                    case 'o': sb.Append("o"); break;
                    case 'O': sb.Append("O"); break;

                    case 'p': sb.Append("k"); break;
                    case 'P': sb.Append("K"); break;

                    case 'q': sb.Append("f"); break;
                    case 'Q': sb.Append("F"); break;

                    case 'r': sb.Append("wy"); break;
                    case 'R': sb.Append("W"); break;

                    case 's': sb.Append("s"); break;
                    case 'S': sb.Append("S"); break;

                    case 't': sb.Append("r"); break;
                    case 'T': sb.Append("R"); break;

                    case 'u': sb.Append("t"); break;
                    case 'U': sb.Append("T"); break;

                    case 'v': sb.Append("z"); break;
                    case 'V': sb.Append("Z"); break;

                    case 'w': sb.Append("el"); break;
                    case 'W': sb.Append("E"); break;

                    case 'x': sb.Append("p"); break;
                    case 'X': sb.Append("P"); break;

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
