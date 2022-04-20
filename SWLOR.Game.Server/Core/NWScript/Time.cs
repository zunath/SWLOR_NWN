namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   * Returns TRUE if it is currently day.
        /// </summary>
        public static bool GetIsDay()
        {
            VM.Call(405);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently night.
        /// </summary>
        public static bool GetIsNight()
        {
            VM.Call(406);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently dawn.
        /// </summary>
        public static bool GetIsDawn()
        {
            VM.Call(407);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently dusk.
        /// </summary>
        public static bool GetIsDusk()
        {
            VM.Call(408);
            return VM.StackPopInt() != 0;
        }


        /// <summary>
        ///   Convert nRounds into a number of seconds
        ///   A round is always 6.0 seconds
        /// </summary>
        public static float RoundsToSeconds(int nRounds)
        {
            VM.StackPush(nRounds);
            VM.Call(121);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Convert nHours into a number of seconds
        ///   The result will depend on how many minutes there are per hour (game-time)
        /// </summary>
        public static float HoursToSeconds(int nHours)
        {
            VM.StackPush(nHours);
            VM.Call(122);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Convert nTurns into a number of seconds
        ///   A turn is always 60.0 seconds
        /// </summary>
        public static float TurnsToSeconds(int nTurns)
        {
            VM.StackPush(nTurns);
            VM.Call(123);
            return VM.StackPopFloat();
        }
    }
}