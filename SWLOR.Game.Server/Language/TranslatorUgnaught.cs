using System.Text;

namespace SWLOR.Game.Server.Language
{
    public class TranslatorUgnaught : ITranslator
    {
        public string Translate(string message)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("b"); break;
                    case 'A': sb.Append("B"); break;

                    case 'b': sb.Append("p"); break;
                    case 'B': sb.Append("P"); break;

                    case 'c': sb.Append("d"); break;
                    case 'C': sb.Append("D"); break;

                    case 'd': sb.Append("w"); break;
                    case 'D': sb.Append("W"); break;

                    case 'e': sb.Append("st"); break;
                    case 'E': sb.Append("S"); break;

                    case 'f': sb.Append("my"); break;
                    case 'F': sb.Append("M"); break;

                    case 'g': sb.Append("z"); break;
                    case 'G': sb.Append("Z"); break;

                    case 'h': sb.Append("j"); break;
                    case 'H': sb.Append("J"); break;

                    case 'i': sb.Append("e"); break;
                    case 'I': sb.Append("E"); break;

                    case 'j': sb.Append("i"); break;
                    case 'J': sb.Append("I"); break;

                    case 'k': sb.Append("u"); break;
                    case 'K': sb.Append("U"); break;

                    case 'l': sb.Append("j"); break;
                    case 'L': sb.Append("J"); break;

                    case 'm': sb.Append("n"); break;
                    case 'M': sb.Append("N"); break;

                    case 'n': sb.Append("g"); break;
                    case 'N': sb.Append("G"); break;

                    case 'o': sb.Append("u"); break;
                    case 'O': sb.Append("U"); break;

                    case 'p': sb.Append("x"); break;
                    case 'P': sb.Append("X"); break;

                    case 'q': sb.Append("a"); break;
                    case 'Q': sb.Append("A"); break;

                    case 'r': sb.Append("l"); break;
                    case 'R': sb.Append("L"); break;

                    case 's': sb.Append("ck"); break;
                    case 'S': sb.Append("C"); break;

                    case 't': sb.Append("v"); break;
                    case 'T': sb.Append("V"); break;

                    case 'u': sb.Append("k"); break;
                    case 'U': sb.Append("K"); break;

                    case 'v': sb.Append("f"); break;
                    case 'V': sb.Append("F"); break;

                    case 'w': sb.Append("h"); break;
                    case 'W': sb.Append("H"); break;

                    case 'x': sb.Append("q"); break;
                    case 'X': sb.Append("Q"); break;

                    case 'y': sb.Append("s"); break;
                    case 'Y': sb.Append("S"); break;

                    case 'z': sb.Append("q"); break;
                    case 'Z': sb.Append("Q"); break;


                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
