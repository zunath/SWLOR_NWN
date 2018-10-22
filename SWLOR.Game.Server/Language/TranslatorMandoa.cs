using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorMandoa : ITranslator
    {
        public string Translate(string message)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("d"); break;
                    case 'A': sb.Append("D"); break;

                    case 'b': sb.Append("q"); break;
                    case 'B': sb.Append("Q"); break;

                    case 'c': sb.Append("av"); break;
                    case 'C': sb.Append("A"); break;

                    case 'd': sb.Append("m"); break;
                    case 'D': sb.Append("M"); break;

                    case 'e': sb.Append("z"); break;
                    case 'E': sb.Append("Z"); break;

                    case 'f': sb.Append("c"); break;
                    case 'F': sb.Append("C"); break;

                    case 'g': sb.Append("r"); break;
                    case 'G': sb.Append("R"); break;

                    case 'h': sb.Append("j"); break;
                    case 'H': sb.Append("J"); break;

                    case 'i': sb.Append("y"); break;
                    case 'I': sb.Append("Y"); break;

                    case 'j': sb.Append("l"); break;
                    case 'J': sb.Append("L"); break;

                    case 'k': sb.Append("i"); break;
                    case 'K': sb.Append("I"); break;

                    case 'l': sb.Append("s"); break;
                    case 'L': sb.Append("S"); break;

                    case 'm': sb.Append("w"); break;
                    case 'M': sb.Append("W"); break;

                    case 'n': sb.Append("x"); break;
                    case 'N': sb.Append("X"); break;

                    case 'o': sb.Append("k"); break;
                    case 'O': sb.Append("K"); break;

                    case 'p': sb.Append("u"); break;
                    case 'P': sb.Append("U"); break;

                    case 'q': sb.Append("e"); break;
                    case 'Q': sb.Append("E"); break;

                    case 'r': sb.Append("t"); break;
                    case 'R': sb.Append("T"); break;

                    case 's': sb.Append("o"); break;
                    case 'S': sb.Append("O"); break;

                    case 't': sb.Append("u"); break;
                    case 'T': sb.Append("U"); break;

                    case 'u': sb.Append("t"); break;
                    case 'U': sb.Append("T"); break;

                    case 'v': sb.Append("b"); break;
                    case 'V': sb.Append("B"); break;

                    case 'w': sb.Append("p"); break;
                    case 'W': sb.Append("P"); break;

                    case 'x': sb.Append("f"); break;
                    case 'X': sb.Append("F"); break;

                    case 'y': sb.Append("g"); break;
                    case 'Y': sb.Append("G"); break;

                    case 'z': sb.Append("h"); break;
                    case 'Z': sb.Append("H"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
