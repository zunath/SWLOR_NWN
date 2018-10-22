using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorBothese : ITranslator
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

                    case 'b': sb.Append("k"); break;
                    case 'B': sb.Append("K"); break;

                    case 'c': sb.Append("t"); break;
                    case 'C': sb.Append("T"); break;

                    case 'd': sb.Append("j"); break;
                    case 'D': sb.Append("J"); break;

                    case 'e': sb.Append("h"); break;
                    case 'E': sb.Append("H"); break;

                    case 'f': sb.Append("g"); break;
                    case 'F': sb.Append("G"); break;

                    case 'g': sb.Append("v"); break;
                    case 'G': sb.Append("V"); break;

                    case 'h': sb.Append("my"); break;
                    case 'H': sb.Append("M"); break;

                    case 'i': sb.Append("s"); break;
                    case 'I': sb.Append("S"); break;

                    case 'j': sb.Append("u"); break;
                    case 'J': sb.Append("U"); break;

                    case 'k': sb.Append("r"); break;
                    case 'K': sb.Append("R"); break;

                    case 'l': sb.Append("l"); break;
                    case 'L': sb.Append("L"); break;

                    case 'm': sb.Append("z"); break;
                    case 'M': sb.Append("Z"); break;

                    case 'n': sb.Append("e"); break;
                    case 'N': sb.Append("E"); break;

                    case 'o': sb.Append("c"); break;
                    case 'O': sb.Append("C"); break;

                    case 'p': sb.Append("c"); break;
                    case 'P': sb.Append("C"); break;

                    case 'q': sb.Append("j"); break;
                    case 'Q': sb.Append("J"); break;

                    case 'r': sb.Append("i"); break;
                    case 'R': sb.Append("I"); break;

                    case 's': sb.Append("o"); break;
                    case 'S': sb.Append("O"); break;

                    case 't': sb.Append("w"); break;
                    case 'T': sb.Append("W"); break;

                    case 'u': sb.Append("q"); break;
                    case 'U': sb.Append("Q"); break;

                    case 'v': sb.Append("b"); break;
                    case 'V': sb.Append("B"); break;

                    case 'w': sb.Append("p"); break;
                    case 'W': sb.Append("P"); break;

                    case 'x': sb.Append("yi"); break;
                    case 'X': sb.Append("Y"); break;

                    case 'y': sb.Append("a"); break;
                    case 'Y': sb.Append("A"); break;

                    case 'z': sb.Append("n"); break;
                    case 'Z': sb.Append("N"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
