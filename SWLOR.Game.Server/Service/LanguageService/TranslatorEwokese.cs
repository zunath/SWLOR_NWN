using System.Text;

namespace SWLOR.Game.Server.Service.LanguageService
{
    public class TranslatorBothese : ITranslator
    {
        public string Translate(string message)
        {
            var sb = new StringBuilder();

            foreach (var ch in message)
            {
                switch (ch)
                {
case 'a': sb.Append("q"); break;
case 'A': sb.Append("Q"); break;

case 'b': sb.Append("'"); break;
case 'B': sb.Append("'"); break;

case 'c': sb.Append("b"); break;
case 'C': sb.Append("B"); break;

case 'd': sb.Append("x"); break;
case 'D': sb.Append("X"); break;

case 'e': sb.Append("h"); break;
case 'E': sb.Append("H"); break;

case 'f': sb.Append("x"); break;
case 'F': sb.Append("X"); break;

case 'g': sb.Append("i"); break;
case 'G': sb.Append("I"); break;

case 'h': sb.Append("y"); break;
case 'H': sb.Append("Y"); break;

case 'i': sb.Append("t"); break;
case 'I': sb.Append("T"); break;

case 'j': sb.Append("r"); break;
case 'J': sb.Append("R"); break;

case 'k': sb.Append("p"); break;
case 'K': sb.Append("P"); break;

case 'l': sb.Append("o"); break;
case 'L': sb.Append("O"); break;

case 'm': sb.Append("m"); break;
case 'M': sb.Append("M"); break;

case 'n': sb.Append("l"); break;
case 'N': sb.Append("L"); break;

case 'o': sb.Append("c"); break;
case 'O': sb.Append("C"); break;

case 'p': sb.Append("u"); break;
case 'P': sb.Append("U"); break;

case 'q': sb.Append("f"); break;
case 'Q': sb.Append("F"); break;

case 'r': sb.Append("g"); break;
case 'R': sb.Append("G"); break;

case 's': sb.Append("k"); break;
case 'S': sb.Append("K"); break;

case 't': sb.Append("n"); break;
case 'T': sb.Append("N"); break;

case 'u': sb.Append("b"); break;
case 'U': sb.Append("B"); break;

case 'v': sb.Append("j"); break;
case 'V': sb.Append("J"); break;

case 'w': sb.Append("w"); break;
case 'W': sb.Append("W"); break;

case 'x': sb.Append("e"); break;
case 'X': sb.Append("E"); break;

case 'y': sb.Append("a"); break;
case 'Y': sb.Append("A"); break;

case 'z': sb.Append("s"); break;
case 'Z': sb.Append("S"); break;

     }
            }

            return sb.ToString();
        }
    }
}
