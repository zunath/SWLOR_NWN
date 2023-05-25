using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Get the length of sString
        ///   * Return value on error: -1
        /// </summary>
        public static int GetStringLength(string sString)
        {
            VM.StackPush(sString);
            VM.Call(59);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Convert sString into upper case
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringUpperCase(string sString)
        {
            VM.StackPush(sString);
            VM.Call(60);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Convert sString into lower case
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringLowerCase(string sString)
        {
            VM.StackPush(sString);
            VM.Call(61);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Get nCount characters from the right end of sString
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringRight(string sString, int nCount)
        {
            VM.StackPush(nCount);
            VM.StackPush(sString);
            VM.Call(62);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Get nCounter characters from the left end of sString
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringLeft(string sString, int nCount)
        {
            VM.StackPush(nCount);
            VM.StackPush(sString);
            VM.Call(63);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Insert sString into sDestination at nPosition
        ///   * Return value on error: ""
        /// </summary>
        public static string InsertString(string sDestination, string sString, int nPosition)
        {
            VM.StackPush(nPosition);
            VM.StackPush(sString);
            VM.StackPush(sDestination);
            VM.Call(64);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Get nCount characters from sString, starting at nStart
        ///   * Return value on error: ""
        /// </summary>
        public static string GetSubString(string sString, int nStart, int nCount)
        {
            VM.StackPush(nCount);
            VM.StackPush(nStart);
            VM.StackPush(sString);
            VM.Call(65);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Find the position of sSubstring inside sString
        ///   - nStart: The character position to start searching at (from the left end of the string).
        ///   * Return value on error: -1
        /// </summary>
        public static int FindSubString(string sString, string sSubString, int nStart = 0)
        {
            VM.StackPush(nStart);
            VM.StackPush(sSubString);
            VM.StackPush(sString);
            VM.Call(66);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   * Returns TRUE if sStringToTest matches sPattern.
        /// </summary>
        public static int TestStringAgainstPattern(string sPattern, string sStringToTest)
        {
            VM.StackPush(sStringToTest);
            VM.StackPush(sPattern);
            VM.Call(177);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the appropriate matched string (this should only be used in
        ///   OnConversation scripts).
        ///   * Returns the appropriate matched string, otherwise returns ""
        /// </summary>
        public static string GetMatchedSubstring(int nString)
        {
            VM.StackPush(nString);
            VM.Call(178);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Get the number of string parameters available.
        ///   * Returns -1 if no string matched (this could be because of a dialogue event)
        /// </summary>
        public static int GetMatchedSubstringsCount()
        {
            VM.Call(179);
            return VM.StackPopInt();
        }

        /// <summary>
        /// Replaces all matching sRegExp in sValue with sReplacement.
        /// * Returns a empty string on error.
        /// * Please see the format documentation for replacement patterns.
        /// * nSyntaxFlags is a mask of REGEXP_*
        /// * nMatchFlags is a mask of REGEXP_MATCH_* and REGEXP_FORMAT_*.
        /// * FORMAT_DEFAULT replacement patterns:
        ///    $$    $
        ///    $&    The matched substring.
        ///    $`    The portion of string that precedes the matched substring.
        ///    $'    The portion of string that follows the matched substring.
        ///    $n    The nth capture, where n is a single digit in the range 1 to 9 and $n is not followed by a decimal digit.
        ///    $nn   The nnth capture, where nn is a two-digit decimal number in the range 01 to 99.
        /// Example: RegExpReplace("a+", "vaaalue", "[$&]")    => "v[aaa]lue"
        /// </summary>
        public static string RegExpReplace(
            string sRegExp,
            string sValue,
            string sReplacement,
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default)
        {
            VM.StackPush((int)nMatchFlags);
            VM.StackPush((int)nSyntaxFlags);
            VM.StackPush(sReplacement);
            VM.StackPush(sValue);
            VM.StackPush(sRegExp);
            VM.Call(1070);

            return VM.StackPopString();
        }
    }
}