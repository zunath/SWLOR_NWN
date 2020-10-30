namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   * Returns TRUE if it is currently day.
        /// </summary>
        public static bool GetIsDay()
        {
            Internal.NativeFunctions.CallBuiltIn(405);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently night.
        /// </summary>
        public static bool GetIsNight()
        {
            Internal.NativeFunctions.CallBuiltIn(406);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently dawn.
        /// </summary>
        public static bool GetIsDawn()
        {
            Internal.NativeFunctions.CallBuiltIn(407);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently dusk.
        /// </summary>
        public static bool GetIsDusk()
        {
            Internal.NativeFunctions.CallBuiltIn(408);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }


        /// <summary>
        ///   Convert nRounds into a number of seconds
        ///   A round is always 6.0 seconds
        /// </summary>
        public static float RoundsToSeconds(int nRounds)
        {
            Internal.NativeFunctions.StackPushInteger(nRounds);
            Internal.NativeFunctions.CallBuiltIn(121);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Convert nHours into a number of seconds
        ///   The result will depend on how many minutes there are per hour (game-time)
        /// </summary>
        public static float HoursToSeconds(int nHours)
        {
            Internal.NativeFunctions.StackPushInteger(nHours);
            Internal.NativeFunctions.CallBuiltIn(122);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Convert nTurns into a number of seconds
        ///   A turn is always 60.0 seconds
        /// </summary>
        public static float TurnsToSeconds(int nTurns)
        {
            Internal.NativeFunctions.StackPushInteger(nTurns);
            Internal.NativeFunctions.CallBuiltIn(123);
            return Internal.NativeFunctions.StackPopFloat();
        }
    }
}