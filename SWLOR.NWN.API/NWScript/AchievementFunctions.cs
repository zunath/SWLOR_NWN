using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Vibrate the player's device or controller. Does nothing if vibration is not supported.
        ///   - nMotor is one of VIBRATOR_MOTOR_*
        ///   - fStrength is between 0.0 and 1.0
        ///   - fSeconds is the number of seconds to vibrate
        /// </summary>
        public static void Vibrate(uint oPlayer, int nMotor, float fStrength, float fSeconds)
        {
            global::NWN.Core.NWScript.Vibrate(oPlayer, nMotor, fStrength, fSeconds);
        }

        /// <summary>
        ///   Unlock an achievement for the given player who must be logged in.
        ///   - sId is the achievement ID on the remote server
        ///   - nLastValue is the previous value of the associated achievement stat
        ///   - nCurValue is the current value of the associated achievement stat
        ///   - nMaxValue is the maximum value of the associate achievement stat
        /// </summary>
        public static void UnlockAchievement(uint oPlayer, string sId, int nLastValue = 0, int nCurValue = 0,
            int nMaxValue = 0)
        {
            global::NWN.Core.NWScript.UnlockAchievement(oPlayer, sId, nLastValue, nCurValue, nMaxValue);
        }
    }
}
