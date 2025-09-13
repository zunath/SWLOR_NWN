namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   * Returns TRUE if it is currently day.
        /// </summary>
        public static bool GetIsDay()
        {
            return NWN.Core.NWScript.GetIsDay() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently night.
        /// </summary>
        public static bool GetIsNight()
        {
            return NWN.Core.NWScript.GetIsNight() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently dawn.
        /// </summary>
        public static bool GetIsDawn()
        {
            return NWN.Core.NWScript.GetIsDawn() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if it is currently dusk.
        /// </summary>
        public static bool GetIsDusk()
        {
            return NWN.Core.NWScript.GetIsDusk() != 0;
        }


        /// <summary>
        ///   Convert nRounds into a number of seconds
        ///   A round is always 6.0 seconds
        /// </summary>
        public static float RoundsToSeconds(int nRounds)
        {
            return NWN.Core.NWScript.RoundsToSeconds(nRounds);
        }

        /// <summary>
        ///   Convert nHours into a number of seconds
        ///   The result will depend on how many minutes there are per hour (game-time)
        /// </summary>
        public static float HoursToSeconds(int nHours)
        {
            return NWN.Core.NWScript.HoursToSeconds(nHours);
        }

        /// <summary>
        ///   Convert nTurns into a number of seconds
        ///   A turn is always 60.0 seconds
        /// </summary>
        public static float TurnsToSeconds(int nTurns)
        {
            return NWN.Core.NWScript.TurnsToSeconds(nTurns);
        }
    }
}