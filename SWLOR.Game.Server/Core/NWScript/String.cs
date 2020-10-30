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
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.CallBuiltIn(59);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Convert sString into upper case
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringUpperCase(string sString)
        {
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.CallBuiltIn(60);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Convert sString into lower case
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringLowerCase(string sString)
        {
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.CallBuiltIn(61);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Get nCount characters from the right end of sString
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringRight(string sString, int nCount)
        {
            Internal.NativeFunctions.StackPushInteger(nCount);
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.CallBuiltIn(62);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Get nCounter characters from the left end of sString
        ///   * Return value on error: ""
        /// </summary>
        public static string GetStringLeft(string sString, int nCount)
        {
            Internal.NativeFunctions.StackPushInteger(nCount);
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.CallBuiltIn(63);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Insert sString into sDestination at nPosition
        ///   * Return value on error: ""
        /// </summary>
        public static string InsertString(string sDestination, string sString, int nPosition)
        {
            Internal.NativeFunctions.StackPushInteger(nPosition);
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.StackPushString(sDestination);
            Internal.NativeFunctions.CallBuiltIn(64);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Get nCount characters from sString, starting at nStart
        ///   * Return value on error: ""
        /// </summary>
        public static string GetSubString(string sString, int nStart, int nCount)
        {
            Internal.NativeFunctions.StackPushInteger(nCount);
            Internal.NativeFunctions.StackPushInteger(nStart);
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.CallBuiltIn(65);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Find the position of sSubstring inside sString
        ///   - nStart: The character position to start searching at (from the left end of the string).
        ///   * Return value on error: -1
        /// </summary>
        public static int FindSubString(string sString, string sSubString, int nStart = 0)
        {
            Internal.NativeFunctions.StackPushInteger(nStart);
            Internal.NativeFunctions.StackPushString(sSubString);
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.CallBuiltIn(66);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   * Returns TRUE if sStringToTest matches sPattern.
        /// </summary>
        public static int TestStringAgainstPattern(string sPattern, string sStringToTest)
        {
            Internal.NativeFunctions.StackPushString(sStringToTest);
            Internal.NativeFunctions.StackPushString(sPattern);
            Internal.NativeFunctions.CallBuiltIn(177);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the appropriate matched string (this should only be used in
        ///   OnConversation scripts).
        ///   * Returns the appropriate matched string, otherwise returns ""
        /// </summary>
        public static string GetMatchedSubstring(int nString)
        {
            Internal.NativeFunctions.StackPushInteger(nString);
            Internal.NativeFunctions.CallBuiltIn(178);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Get the number of string parameters available.
        ///   * Returns -1 if no string matched (this could be because of a dialogue event)
        /// </summary>
        public static int GetMatchedSubstringsCount()
        {
            Internal.NativeFunctions.CallBuiltIn(179);
            return Internal.NativeFunctions.StackPopInteger();
        }
    }
}