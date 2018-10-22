using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorTwileki : ITranslator
    {
        public string Translate(string message)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("z"); break;
                    case 'A': sb.Append("Z"); break;

                    case 'b': sb.Append("t"); break;
                    case 'B': sb.Append("T"); break;

                    case 'c': sb.Append("p"); break;
                    case 'C': sb.Append("P"); break;

                    case 'd': sb.Append("c"); break;
                    case 'D': sb.Append("C"); break;

                    case 'e': sb.Append("x"); break;
                    case 'E': sb.Append("X"); break;

                    case 'f': sb.Append("h"); break;
                    case 'F': sb.Append("H"); break;

                    case 'g': sb.Append("uy"); break;
                    case 'G': sb.Append("U"); break;

                    case 'h': sb.Append("r"); break;
                    case 'H': sb.Append("R"); break;

                    case 'i': sb.Append("i"); break;
                    case 'I': sb.Append("I"); break;

                    case 'j': sb.Append("g"); break;
                    case 'J': sb.Append("G"); break;

                    case 'k': sb.Append("f"); break;
                    case 'K': sb.Append("F"); break;

                    case 'l': sb.Append("e"); break;
                    case 'L': sb.Append("E"); break;

                    case 'm': sb.Append("q"); break;
                    case 'M': sb.Append("Q"); break;

                    case 'n': sb.Append("'"); break;
                    case 'N': sb.Append("'"); break;

                    case 'o': sb.Append("v"); break;
                    case 'O': sb.Append("V"); break;

                    case 'p': sb.Append("j"); break;
                    case 'P': sb.Append("J"); break;

                    case 'q': sb.Append("b"); break;
                    case 'Q': sb.Append("B"); break;

                    case 'r': sb.Append("a"); break;
                    case 'R': sb.Append("A"); break;

                    case 's': sb.Append("g"); break;
                    case 'S': sb.Append("G"); break;

                    case 't': sb.Append("k"); break;
                    case 'T': sb.Append("K"); break;

                    case 'u': sb.Append("s"); break;
                    case 'U': sb.Append("S"); break;

                    case 'v': sb.Append("wo"); break;
                    case 'V': sb.Append("W"); break;

                    case 'w': sb.Append("m"); break;
                    case 'W': sb.Append("M"); break;

                    case 'x': sb.Append("q"); break;
                    case 'X': sb.Append("Q"); break;

                    case 'y': sb.Append("n"); break;
                    case 'Y': sb.Append("N"); break;

                    case 'z': sb.Append("d"); break;
                    case 'Z': sb.Append("D"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
