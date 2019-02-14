using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorMirialan : ITranslator
    {
        public string Translate(string message)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("v"); break;
                    case 'A': sb.Append("V"); break;

                    case 'b': sb.Append("q"); break;
                    case 'B': sb.Append("Q"); break;

                    case 'c': sb.Append("x"); break;
                    case 'C': sb.Append("X"); break;

                    case 'd': sb.Append("t"); break;
                    case 'D': sb.Append("T"); break;

                    case 'e': sb.Append("j"); break;
                    case 'E': sb.Append("J"); break;

                    case 'f': sb.Append("e"); break;
                    case 'F': sb.Append("E"); break;

                    case 'g': sb.Append("o"); break;
                    case 'G': sb.Append("O"); break;

                    case 'h': sb.Append("k"); break;
                    case 'H': sb.Append("K"); break;

                    case 'i': sb.Append("u"); break;
                    case 'I': sb.Append("U"); break;

                    case 'j': sb.Append("i"); break;
                    case 'J': sb.Append("I"); break;

                    case 'k': sb.Append("r"); break;
                    case 'K': sb.Append("R"); break;

                    case 'l': sb.Append("f"); break;
                    case 'L': sb.Append("F"); break;

                    case 'm': sb.Append("p"); break;
                    case 'M': sb.Append("P"); break;

                    case 'n': sb.Append("w"); break;
                    case 'N': sb.Append("W"); break;

                    case 'o': sb.Append("g"); break;
                    case 'O': sb.Append("G"); break;

                    case 'p': sb.Append("'"); break;
                    case 'P': sb.Append("'"); break;

                    case 'q': sb.Append("p"); break;
                    case 'Q': sb.Append("P"); break;

                    case 'r': sb.Append("c"); break;
                    case 'R': sb.Append("C"); break;

                    case 's': sb.Append("a"); break;
                    case 'S': sb.Append("A"); break;

                    case 't': sb.Append("n"); break;
                    case 'T': sb.Append("N"); break;

                    case 'u': sb.Append("b"); break;
                    case 'U': sb.Append("B"); break;

                    case 'v': sb.Append("s"); break;
                    case 'V': sb.Append("S"); break;

                    case 'w': sb.Append("d"); break;
                    case 'W': sb.Append("D"); break;

                    case 'x': sb.Append("y"); break;
                    case 'X': sb.Append("Y"); break;

                    case 'y': sb.Append("l"); break;
                    case 'Y': sb.Append("L"); break;

                    case 'z': sb.Append("m"); break;
                    case 'Z': sb.Append("M"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
