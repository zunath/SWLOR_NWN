using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {

        /// <summary>
        /// Gets the length of the string.
        /// </summary>
        /// <param name="sString">The string to get the length of</param>
        /// <returns>The length of the string, or -1 on error</returns>
        public static int GetStringLength(string sString)
        {
            return NWScriptRuntime.Current.GetStringLength(sString);
        }

        /// <summary>
        /// Converts the string to upper case.
        /// </summary>
        /// <param name="sString">The string to convert</param>
        /// <returns>The string in upper case, or empty string on error</returns>
        public static string GetStringUpperCase(string sString)
        {
            return NWScriptRuntime.Current.GetStringUpperCase(sString);
        }

        /// <summary>
        /// Converts the string to lower case.
        /// </summary>
        /// <param name="sString">The string to convert</param>
        /// <returns>The string in lower case, or empty string on error</returns>
        public static string GetStringLowerCase(string sString)
        {
            return NWScriptRuntime.Current.GetStringLowerCase(sString);
        }

        /// <summary>
        /// Gets the specified number of characters from the right end of the string.
        /// </summary>
        /// <param name="sString">The string to get characters from</param>
        /// <param name="nCount">The number of characters to get from the right end</param>
        /// <returns>The rightmost characters, or empty string on error</returns>
        public static string GetStringRight(string sString, int nCount)
        {
            return NWScriptRuntime.Current.GetStringRight(sString, nCount);
        }

        /// <summary>
        /// Gets the specified number of characters from the left end of the string.
        /// </summary>
        /// <param name="sString">The string to get characters from</param>
        /// <param name="nCount">The number of characters to get from the left end</param>
        /// <returns>The leftmost characters, or empty string on error</returns>
        public static string GetStringLeft(string sString, int nCount)
        {
            return NWScriptRuntime.Current.GetStringLeft(sString, nCount);
        }

        /// <summary>
        /// Inserts a string into the destination string at the specified position.
        /// </summary>
        /// <param name="sDestination">The destination string to insert into</param>
        /// <param name="sString">The string to insert</param>
        /// <param name="nPosition">The position to insert at</param>
        /// <returns>The resulting string, or empty string on error</returns>
        public static string InsertString(string sDestination, string sString, int nPosition)
        {
            return NWScriptRuntime.Current.InsertString(sDestination, sString, nPosition);
        }

        /// <summary>
        /// Gets the specified number of characters from the string, starting at the specified position.
        /// </summary>
        /// <param name="sString">The string to get characters from</param>
        /// <param name="nStart">The starting position</param>
        /// <param name="nCount">The number of characters to get</param>
        /// <returns>The substring, or empty string on error</returns>
        public static string GetSubString(string sString, int nStart, int nCount)
        {
            return NWScriptRuntime.Current.GetSubString(sString, nStart, nCount);
        }

        /// <summary>
        /// Finds the position of the substring inside the string.
        /// </summary>
        /// <param name="sString">The string to search in</param>
        /// <param name="sSubString">The substring to search for</param>
        /// <param name="nStart">The character position to start searching at (from the left end of the string)</param>
        /// <returns>The position of the substring, or -1 on error</returns>
        public static int FindSubString(string sString, string sSubString, int nStart = 0)
        {
            return NWScriptRuntime.Current.FindSubString(sString, sSubString, nStart);
        }

        /// <summary>
        /// Tests if the string matches the pattern.
        /// </summary>
        /// <param name="sPattern">The pattern to match against</param>
        /// <param name="sStringToTest">The string to test</param>
        /// <returns>TRUE if the string matches the pattern</returns>
        public static bool TestStringAgainstPattern(string sPattern, string sStringToTest)
        {
            return NWScriptRuntime.Current.TestStringAgainstPattern(sPattern, sStringToTest);
        }

        /// <summary>
        /// Gets the appropriate matched string (this should only be used in OnConversation scripts).
        /// </summary>
        /// <param name="nString">The string index</param>
        /// <returns>The appropriate matched string, otherwise returns empty string</returns>
        public static string GetMatchedSubstring(int nString)
        {
            return NWScriptRuntime.Current.GetMatchedSubstring(nString);
        }

        /// <summary>
        /// Gets the number of string parameters available.
        /// </summary>
        /// <returns>The number of string parameters available, or -1 if no string matched (this could be because of a dialogue event)</returns>
        public static int GetMatchedSubstringsCount()
        {
            return NWScriptRuntime.Current.GetMatchedSubstringsCount();
        }

        /// <summary>
        /// Replaces all matching regular expression in the value with the replacement.
        /// Returns an empty string on error.
        /// Please see the format documentation for replacement patterns.
        /// FORMAT_DEFAULT replacement patterns:
        /// $$    $
        /// $&    The matched substring.
        /// $`    The portion of string that precedes the matched substring.
        /// $'    The portion of string that follows the matched substring.
        /// $n    The nth capture, where n is a single digit in the range 1 to 9 and $n is not followed by a decimal digit.
        /// $nn   The nnth capture, where nn is a two-digit decimal number in the range 01 to 99.
        /// Example: RegExpReplace("a+", "vaaalue", "[$&]")    => "v[aaa]lue"
        /// </summary>
        /// <param name="sRegExp">The regular expression to match</param>
        /// <param name="sValue">The string to search and replace in</param>
        /// <param name="sReplacement">The replacement string</param>
        /// <param name="nSyntaxFlags">A mask of REGEXP_* constants</param>
        /// <param name="nMatchFlags">A mask of REGEXP_MATCH_* and REGEXP_FORMAT_* constants</param>
        /// <returns>The string with replacements made, or empty string on error</returns>
        public static string RegExpReplace(
            string sRegExp,
            string sValue,
            string sReplacement,
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default)
        {
            return NWScriptRuntime.Current.RegExpReplace(sRegExp, sValue, sReplacement, nSyntaxFlags, nMatchFlags);
        }

        /// <summary>
        /// Overrides a given strref to always return the specified value instead of what is in the TLK file.
        /// Setting sValue to empty string will delete the override.
        /// </summary>
        /// <param name="nStrRef">The string reference to override</param>
        /// <param name="sValue">The value to return instead of the TLK file value (empty string to delete override)</param>
        public static void SetTlkOverride(int nStrRef, string sValue = "")
        {
            NWScriptRuntime.Current.SetTlkOverride(nStrRef, sValue);
        }

        /// <summary>
        /// Converts a float into a string.
        /// </summary>
        /// <param name="fFloat">The float to convert</param>
        /// <param name="nWidth">Should be a value from 0 to 18 inclusive</param>
        /// <param name="nDecimals">Should be a value from 0 to 9 inclusive</param>
        /// <returns>The float as a string</returns>
        public static string FloatToString(float fFloat, int nWidth = 18, int nDecimals = 9)
        {
            return NWScriptRuntime.Current.FloatToString(fFloat, nWidth, nDecimals);
        }

        /// <summary>
        /// Converts an integer into a string.
        /// </summary>
        /// <param name="nInteger">The integer to convert</param>
        /// <returns>The integer as a string, or empty string on error</returns>
        public static string IntToString(int nInteger)
        {
            return NWScriptRuntime.Current.IntToString(nInteger);
        }

        /// <summary>
        /// Gets a string from the talk table using the string reference.
        /// </summary>
        /// <param name="nStrRef">The string reference to look up</param>
        /// <param name="nGender">The gender for gender-specific strings</param>
        /// <returns>The string from the talk table</returns>
        public static string GetStringByStrRef(int nStrRef, Gender nGender = Gender.Male)
        {
            return NWScriptRuntime.Current.GetStringByStrRef(nStrRef, nGender);
        }

        /// <summary>
        /// Generates a random name.
        /// </summary>
        /// <param name="nNameType">The type of random name to be generated (NAME_*)</param>
        /// <returns>A random name of the specified type</returns>
        public static string RandomName(Name nNameType = Name.FirstGenericMale)
        {
            return NWScriptRuntime.Current.RandomName(nNameType);
        }

        /// <summary>
        /// Sets the value for a custom token.
        /// </summary>
        /// <param name="nCustomTokenNumber">The custom token number</param>
        /// <param name="sTokenValue">The value to set for the token</param>
        public static void SetCustomToken(int nCustomTokenNumber, string sTokenValue)
        {
            NWScriptRuntime.Current.SetCustomToken(nCustomTokenNumber, sTokenValue);
        }
    }
}
