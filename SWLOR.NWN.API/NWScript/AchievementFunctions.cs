namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Vibrates the player's device or controller. Does nothing if vibration is not supported.
        /// </summary>
        /// <param name="oPlayer">The player whose device should vibrate</param>
        /// <param name="nMotor">One of the VIBRATOR_MOTOR_* constants</param>
        /// <param name="fStrength">Vibration strength between 0.0 and 1.0</param>
        /// <param name="fSeconds">Number of seconds to vibrate</param>
        public static void Vibrate(uint oPlayer, int nMotor, float fStrength, float fSeconds)
        {
            global::NWN.Core.NWScript.Vibrate(oPlayer, nMotor, fStrength, fSeconds);
        }

        /// <summary>
        /// Unlocks an achievement for the given player who must be logged in.
        /// </summary>
        /// <param name="oPlayer">The player for whom to unlock the achievement</param>
        /// <param name="sId">The achievement ID on the remote server</param>
        /// <param name="nLastValue">The previous value of the associated achievement stat (default: 0)</param>
        /// <param name="nCurValue">The current value of the associated achievement stat (default: 0)</param>
        /// <param name="nMaxValue">The maximum value of the associated achievement stat (default: 0)</param>
        public static void UnlockAchievement(uint oPlayer, string sId, int nLastValue = 0, int nCurValue = 0,
            int nMaxValue = 0)
        {
            global::NWN.Core.NWScript.UnlockAchievement(oPlayer, sId, nLastValue, nCurValue, nMaxValue);
        }
    }
}
