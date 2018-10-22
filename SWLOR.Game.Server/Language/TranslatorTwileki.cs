using System;
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
                    case 'a': sb.Append("ca"); break;
                    case 'A': sb.Append("C"); break;

                    case 'b': sb.Append("z"); break;
                    case 'B': sb.Append("Z"); break;

                    case 'c': sb.Append("h"); break;
                    case 'C': sb.Append("H"); break;

                    case 'd': sb.Append("'"); break;
                    case 'D': sb.Append("Q"); break;

                    case 'e': sb.Append("a"); break;
                    case 'E': sb.Append("A"); break;

                    case 'f': sb.Append("f"); break;
                    case 'F': sb.Append("F"); break;

                    case 'g': sb.Append("zn"); break;
                    case 'G': sb.Append("Z"); break;

                    case 'h': sb.Append("g"); break;
                    case 'H': sb.Append("G"); break;

                    case 'i': sb.Append("e"); break;
                    case 'I': sb.Append("E"); break;

                    case 'j': sb.Append("n"); break;
                    case 'J': sb.Append("N"); break;

                    case 'k': sb.Append("ka"); break;
                    case 'K': sb.Append("K"); break;

                    case 'l': sb.Append("ca"); break;
                    case 'L': sb.Append("C"); break;

                    case 'm': sb.Append("x"); break;
                    case 'M': sb.Append("X"); break;

                    case 'n': sb.Append("k"); break;
                    case 'N': sb.Append("K"); break;

                    case 'o': sb.Append("p"); break;
                    case 'O': sb.Append("P"); break;

                    case 'p': sb.Append("l"); break;
                    case 'P': sb.Append("L"); break;

                    case 'q': sb.Append("r"); break;
                    case 'Q': sb.Append("R"); break;

                    case 'r': sb.Append("j"); break;
                    case 'R': sb.Append("J"); break;

                    case 's': sb.Append("h"); break;
                    case 'S': sb.Append("H"); break;

                    case 't': sb.Append("u"); break;
                    case 'T': sb.Append("U"); break;

                    case 'u': sb.Append("t"); break;
                    case 'U': sb.Append("T"); break;

                    case 'v': sb.Append("s"); break;
                    case 'V': sb.Append("S"); break;

                    case 'w': sb.Append("m"); break;
                    case 'W': sb.Append("M"); break;

                    case 'x': sb.Append("sk"); break;
                    case 'X': sb.Append("S"); break;

                    case 'y': sb.Append("b"); break;
                    case 'Y': sb.Append("B"); break;

                    case 'z': sb.Append("vz"); break;
                    case 'Z': sb.Append("V"); break;

                    default: sb.Append(ch); break;
                }
            }

            return sb.ToString();
        }
    }
}
