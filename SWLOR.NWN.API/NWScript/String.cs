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
            return NWN.Core.NWScript.GetStringLength(sString);
        }

        /// <summary>
        ///   Convert sString into upper case
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringUpperCase(string sString)
        {
            return NWN.Core.NWScript.GetStringUpperCase(sString);
        }

        /// <summary>
        ///   Convert sString into lower case
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringLowerCase(string sString)
        {
            return NWN.Core.NWScript.GetStringLowerCase(sString);
        }

        /// <summary>
        ///   Get nCount characters from the right end of sString
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringRight(string sString, int nCount)
        {
            return NWN.Core.NWScript.GetStringRight(sString, nCount);
        }

        /// <summary>
        ///   Get nCounter characters from the left end of sString
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringLeft(string sString, int nCount)
        {
            return NWN.Core.NWScript.GetStringLeft(sString, nCount);
        }

        /// <summary>
        ///   Insert sString into sDestination at nPosition
        ///   * Return value on error: ""
        /// </summary>
        public static string InsertString(string sDestination, string sString, int nPosition)
        {
            return NWN.Core.NWScript.InsertString(sDestination, sString, nPosition);
        }

        /// <summary>
        ///   Get nCount characters from sString, starting at nStart
        ///   * Return value on error: ""
        /// </summary>
        public static string GetSubString(string sString, int nStart, int nCount)
        {
            return NWN.Core.NWScript.GetSubString(sString, nStart, nCount);
        }

        /// <summary>
        ///   Find the position of sSubstring inside sString
        ///   - nStart: The character position to start searching at (from the left end of the string).
        ///   * Return value on error: -1
        /// </summary>
        public static int FindSubString(string sString, string sSubString, int nStart = 0)
        {
            return NWN.Core.NWScript.FindSubString(sString, sSubString, nStart);
        }

        /// <summary>
        ///   * Returns TRUE if sStringToTest matches sPattern.
        /// </summary>
        public static bool TestStringAgainstPattern(string sPattern, string sStringToTest)
        {
            return NWN.Core.NWScript.TestStringAgainstPattern(sPattern, sStringToTest) != 0;
        }

        /// <summary>
        ///   Get the appropriate matched string (this should only be used in
        ///   OnConversation scripts).
        ///   * Returns the appropriate matched string, otherwise returns ""
        /// </summary>
        public static string GetMatchedSubstring(int nString)
        {
            return NWN.Core.NWScript.GetMatchedSubstring(nString);
        }

        /// <summary>
        ///   Get the number of string parameters available.
        ///   * Returns -1 if no string matched (this could be because of a dialogue event)
        /// </summary>
        public static int GetMatchedSubstringsCount()
        {
            return NWN.Core.NWScript.GetMatchedSubstringsCount();
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
            return NWN.Core.NWScript.RegExpReplace(sRegExp, sValue, sReplacement, (int)nSyntaxFlags, (int)nMatchFlags);
        }
    }
}