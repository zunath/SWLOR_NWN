namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Checks if it is currently day.
        /// </summary>
        /// <returns>TRUE if it is currently day</returns>
        public static bool GetIsDay()
        {
            return global::NWN.Core.NWScript.GetIsDay() != 0;
        }

        /// <summary>
        /// Checks if it is currently night.
        /// </summary>
        /// <returns>TRUE if it is currently night</returns>
        public static bool GetIsNight()
        {
            return global::NWN.Core.NWScript.GetIsNight() != 0;
        }

        /// <summary>
        /// Checks if it is currently dawn.
        /// </summary>
        /// <returns>TRUE if it is currently dawn</returns>
        public static bool GetIsDawn()
        {
            return global::NWN.Core.NWScript.GetIsDawn() != 0;
        }

        /// <summary>
        /// Checks if it is currently dusk.
        /// </summary>
        /// <returns>TRUE if it is currently dusk</returns>
        public static bool GetIsDusk()
        {
            return global::NWN.Core.NWScript.GetIsDusk() != 0;
        }


        /// <summary>
        /// Converts rounds into a number of seconds.
        /// A round is always 6.0 seconds.
        /// </summary>
        /// <param name="nRounds">The number of rounds to convert</param>
        /// <returns>The number of seconds</returns>
        public static float RoundsToSeconds(int nRounds)
        {
            return global::NWN.Core.NWScript.RoundsToSeconds(nRounds);
        }

        /// <summary>
        /// Converts hours into a number of seconds.
        /// The result will depend on how many minutes there are per hour (game-time).
        /// </summary>
        /// <param name="nHours">The number of hours to convert</param>
        /// <returns>The number of seconds</returns>
        public static float HoursToSeconds(int nHours)
        {
            return global::NWN.Core.NWScript.HoursToSeconds(nHours);
        }

        /// <summary>
        /// Converts turns into a number of seconds.
        /// A turn is always 60.0 seconds.
        /// </summary>
        /// <param name="nTurns">The number of turns to convert</param>
        /// <returns>The number of seconds</returns>
        public static float TurnsToSeconds(int nTurns)
        {
            return global::NWN.Core.NWScript.TurnsToSeconds(nTurns);
        }
    }
}