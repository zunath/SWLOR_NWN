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
            return NWScriptRuntime.Current.GetIsDay();
        }

        /// <summary>
        /// Checks if it is currently night.
        /// </summary>
        /// <returns>TRUE if it is currently night</returns>
        public static bool GetIsNight()
        {
            return NWScriptRuntime.Current.GetIsNight();
        }

        /// <summary>
        /// Checks if it is currently dawn.
        /// </summary>
        /// <returns>TRUE if it is currently dawn</returns>
        public static bool GetIsDawn()
        {
            return NWScriptRuntime.Current.GetIsDawn();
        }

        /// <summary>
        /// Checks if it is currently dusk.
        /// </summary>
        /// <returns>TRUE if it is currently dusk</returns>
        public static bool GetIsDusk()
        {
            return NWScriptRuntime.Current.GetIsDusk();
        }


        /// <summary>
        /// Converts rounds into a number of seconds.
        /// A round is always 6.0 seconds.
        /// </summary>
        /// <param name="nRounds">The number of rounds to convert</param>
        /// <returns>The number of seconds</returns>
        public static float RoundsToSeconds(int nRounds)
        {
            return NWScriptRuntime.Current.RoundsToSeconds(nRounds);
        }

        /// <summary>
        /// Converts hours into a number of seconds.
        /// The result will depend on how many minutes there are per hour (game-time).
        /// </summary>
        /// <param name="nHours">The number of hours to convert</param>
        /// <returns>The number of seconds</returns>
        public static float HoursToSeconds(int nHours)
        {
            return NWScriptRuntime.Current.HoursToSeconds(nHours);
        }

        /// <summary>
        /// Converts turns into a number of seconds.
        /// A turn is always 60.0 seconds.
        /// </summary>
        /// <param name="nTurns">The number of turns to convert</param>
        /// <returns>The number of seconds</returns>
        public static float TurnsToSeconds(int nTurns)
        {
            return NWScriptRuntime.Current.TurnsToSeconds(nTurns);
        }

        /// <summary>
        /// Gets the current hour (0-23).
        /// </summary>
        /// <returns>The current hour</returns>
        public static int GetTimeHour()
        {
            return NWScriptRuntime.Current.GetTimeHour();
        }

        /// <summary>
        /// Gets the current minute (0-59).
        /// </summary>
        /// <returns>The current minute</returns>
        public static int GetTimeMinute()
        {
            return NWScriptRuntime.Current.GetTimeMinute();
        }

        /// <summary>
        /// Gets the current second (0-59).
        /// </summary>
        /// <returns>The current second</returns>
        public static int GetTimeSecond()
        {
            return NWScriptRuntime.Current.GetTimeSecond();
        }

        /// <summary>
        /// Gets the current millisecond (0-999).
        /// </summary>
        /// <returns>The current millisecond</returns>
        public static int GetTimeMillisecond()
        {
            return NWScriptRuntime.Current.GetTimeMillisecond();
        }
    }
}
