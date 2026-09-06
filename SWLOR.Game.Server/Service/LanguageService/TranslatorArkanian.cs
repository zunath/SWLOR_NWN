using System.Text;

namespace SWLOR.Game.Server.Service.LanguageService
{
    public class TranslatorArkanian : ITranslator
    {
        public string Translate(string message)
        {
            var sb = new StringBuilder();

            foreach (var ch in message)
            {
                switch (ch)
                {
                    case 'a': sb.Append("ae"); break;
                    case 'A': sb.Append("Ae"); break;

                    case 'b': sb.Append("v"); break;
                    case 'B': sb.Append("V"); break;

                    case 'c': sb.Append("k"); break;
                    case 'C': sb.Append("K"); break;

                    case 'd': sb.Append("th"); break;
                    case 'D': sb.Append("Th"); break;

                    case 'e': sb.Append("i"); break;
                    case 'E': sb.Append("I"); break;

                    case 'f': sb.Append("ph"); break;
                    case 'F': sb.Append("Ph"); break;

                    case 'g': sb.Append("d"); break;
                    case 'G': sb.Append("D"); break;

                    case 'h': sb.Append("sh"); break;
                    case 'H': sb.Append("Sh"); break;

                    case 'i': sb.Append("ei"); break;
                    case 'I': sb.Append("Ei"); break;

                    case 'j': sb.Append("zh"); break;
                    case 'J': sb.Append("Zh"); break;

                    case 'k': sb.Append("q"); break;
                    case 'K': sb.Append("Q"); break;

                    case 'l': sb.Append("r"); break;
                    case 'L': sb.Append("R"); break;

                    case 'm': sb.Append("n"); break;
                    case 'M': sb.Append("N"); break;

                    case 'n': sb.Append("l"); break;
                    case 'N': sb.Append("L"); break;

                    case 'o': sb.Append("au"); break;
                    case 'O': sb.Append("Au"); break;

                    case 'p': sb.Append("b"); break;
                    case 'P': sb.Append("B"); break;

                    case 'q': sb.Append("kh"); break;
                    case 'Q': sb.Append("Kh"); break;

                    case 'r': sb.Append("s"); break;
                    case 'R': sb.Append("S"); break;

                    case 's': sb.Append("z"); break;
                    case 'S': sb.Append("Z"); break;

                    case 't': sb.Append("t"); break;
                    case 'T': sb.Append("T"); break;

                    case 'u': sb.Append("u"); break;
                    case 'U': sb.Append("U"); break;

                    case 'v': sb.Append("f"); break;
                    case 'V': sb.Append("F"); break;

                    case 'w': sb.Append("v"); break;
                    case 'W': sb.Append("V"); break;

                    case 'x': sb.Append("ks"); break;
                    case 'X': sb.Append("Ks"); break;

                    case 'y': sb.Append("ia"); break;
                    case 'Y': sb.Append("Ia"); break;

                    case 'z': sb.Append("j"); break;
                    case 'Z': sb.Append("J"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
