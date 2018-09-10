using System;
using System.Text;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    ///      *
    ///      * Access to the color tokens provided by The Krit.
    ///      ************************************************************
    ///      * Please use these judiciously to enhance the gaming
    ///      * experience. (Overuse of colors detracts from it.)
    ///      ************************************************************
    ///      * Color tokens in a String will change the color from that
    ///      * point on when the String is displayed on the screen.
    ///      * Every color change should be ended by an end token,
    ///      * supplied by ColorTokenEnd().
    ///      ************************************************************/
    /// </summary>
    public class ColorTokenService : IColorTokenService
    {
        private static string ColorArray => "     !##$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]]^_`abcdefghijklmnopqrstuvwxyz{|}~€‚ƒ„…†‡ˆ‰Š‹ŒŽ‘’“”•–—˜™š›œžŸ ¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþþ";

        public string TokenStart(byte red, byte green, byte blue)
        {
            if (red > 255) throw new ArgumentOutOfRangeException(nameof(red), "Red must be between 0 and 255.");
            if (green > 255) throw new ArgumentOutOfRangeException(nameof(green), "Green must be between 0 and 255.");
            if (blue > 255) throw new ArgumentOutOfRangeException(nameof(blue), "Blue must be between 0 and 255.");
            
            return "<c" + 
                   ColorArray.Substring(red, 1) +
                   ColorArray.Substring(green, 1) +
                   ColorArray.Substring(blue, 1) +
                   ">";
        }

        public string Custom(string text, byte red, byte green, byte blue)
        {
            if (red > 255) throw new ArgumentOutOfRangeException(nameof(red), "Red must be between 0 and 255.");
            if (green > 255) throw new ArgumentOutOfRangeException(nameof(green), "Green must be between 0 and 255.");
            if (blue > 255) throw new ArgumentOutOfRangeException(nameof(blue), "Blue must be between 0 and 255.");
            if(string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(red, green, blue) + text + TokenEnd();
        }

        public string TokenEnd()
        {
            return "</c>";
        }
        
        public string Black(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(0, 0, 0) + text + TokenEnd();
        }
        public string Blue(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(0, 0, 255) + text + TokenEnd();
        }

        public string Gray(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(127, 127, 127) + text + TokenEnd();
        }

        public string Green(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(0, 255, 0) + text + TokenEnd();
        }

        public string LightPurple(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));
            
            return TokenStart(175, 48, 255) + text + TokenEnd();
        }

        public string Orange(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(255, 127, 0) + text + TokenEnd();
        }

        public string Pink(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(255, 0, 255) + text + TokenEnd();
        }

        public string Purple(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(127, 0, 255) + text + TokenEnd();
        }

        public string Red(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(255, 0, 0) + text + TokenEnd();
        }

        public string White(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(255, 255, 255) + text + TokenEnd();
        }

        public string Yellow(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(255, 255, 0) + text + TokenEnd();
        }

        public string Cyan(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(0, 255, 255) + text + TokenEnd();
        }

        public string Combat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(255, 102, 0) + text + TokenEnd();
        }

        public string Dialog(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(255, 255, 255) + text + TokenEnd();
        }

        public string DialogAction(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(1, 254, 1) + text + TokenEnd();
        }

        public string DialogCheck(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(254, 1, 1) + text + TokenEnd();
        }

        public string DialogHighlight(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(1, 1, 254) + text + TokenEnd();
        }

        public string DialogReply(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));
            
            return TokenStart(102, 178, 255) + text + TokenEnd();
        }

        public string DM(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));
            
            return TokenStart(16, 223, 255) + text + TokenEnd();
        }

        public string GameEngine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));
            
            return TokenStart(204, 119, 255) + text + TokenEnd();
        }

        public string SavingThrow(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));
            
            return TokenStart(102, 204, 255) + text + TokenEnd();
        }

        public string Script(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(255, 255, 0) + text + TokenEnd();
        }

        public string Server(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(176, 176, 176) + text + TokenEnd();
        }

        public string Shout(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));
            
            return TokenStart(255, 239, 80) + text + TokenEnd();
        }

        public string SkillCheck(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));
            
            return TokenStart(0, 102, 255) + text + TokenEnd();
        }

        public string Talk(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(240, 240, 240) + text + TokenEnd();
        }

        public string Tell(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(32, 255, 32) + text + TokenEnd();
        }

        public string Whisper(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be null, empty, or white space.", nameof(text));

            return TokenStart(128, 128, 128) + text + TokenEnd();
        }

        ///////////////////////////////////////////////////////////////////////////////
        // _.GetNamePCColor()
        //
        // Returns the name of oPC, surrounded by color tokens, so the color of
        // the name is the lighter blue often used in NWN game engine messages.
        //
        //
        public string GetNamePCColor(NWObject oPC)
        {
            if (oPC == null) throw new ArgumentNullException(nameof(oPC), nameof(oPC) + " cannot be null.");
            if (oPC.Object == null) throw new ArgumentNullException(nameof(oPC.Object), nameof(oPC.Object) + " cannot be null.");
            
            return TokenStart(153, 255, 255) + oPC.Name + TokenEnd();
        }

        ///////////////////////////////////////////////////////////////////////////////
        // _.GetNameNPCColor()
        //
        // Returns the name of oNPC, surrounded by color tokens, so the color of
        // the name is the shade of purple often used in NWN game engine messages.
        //
        public string GetNameNPCColor(NWObject oNPC)
        {
            if (oNPC == null) throw new ArgumentNullException(nameof(oNPC), nameof(oNPC) + " cannot be null.");
            if (oNPC.Object == null) throw new ArgumentNullException(nameof(oNPC.Object), nameof(oNPC.Object) + " cannot be null.");

            return TokenStart(204, 153, 204) + oNPC.Name + TokenEnd();
        }

    }
}
