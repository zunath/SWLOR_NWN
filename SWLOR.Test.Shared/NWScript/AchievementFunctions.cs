using System.Collections.Generic;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for achievements and vibration
        private readonly Dictionary<uint, HashSet<string>> _unlockedAchievements = new();
        private readonly List<VibrationRecord> _vibrationHistory = new();

        private class VibrationRecord
        {
            public uint Player { get; set; }
            public int Motor { get; set; }
            public float Strength { get; set; }
            public float Seconds { get; set; }
        }

        /// <summary>
        /// Vibrates the player's device or controller. Does nothing if vibration is not supported.
        /// </summary>
        /// <param name="oPlayer">The player whose device should vibrate</param>
        /// <param name="nMotor">One of the VIBRATOR_MOTOR_* constants</param>
        /// <param name="fStrength">Vibration strength between 0.0 and 1.0</param>
        /// <param name="fSeconds">Number of seconds to vibrate</param>
        public void Vibrate(uint oPlayer, int nMotor, float fStrength, float fSeconds)
        {
            _vibrationHistory.Add(new VibrationRecord
            {
                Player = oPlayer,
                Motor = nMotor,
                Strength = fStrength,
                Seconds = fSeconds
            });
        }

        /// <summary>
        /// Unlocks an achievement for the given player who must be logged in.
        /// </summary>
        /// <param name="oPlayer">The player for whom to unlock the achievement</param>
        /// <param name="sId">The achievement ID on the remote server</param>
        /// <param name="nLastValue">The previous value of the associated achievement stat (default: 0)</param>
        /// <param name="nCurValue">The current value of the associated achievement stat (default: 0)</param>
        /// <param name="nMaxValue">The maximum value of the associated achievement stat (default: 0)</param>
        public void UnlockAchievement(uint oPlayer, string sId, int nLastValue = 0, int nCurValue = 0,
            int nMaxValue = 0)
        {
            if (!_unlockedAchievements.ContainsKey(oPlayer))
                _unlockedAchievements[oPlayer] = new HashSet<string>();
            _unlockedAchievements[oPlayer].Add(sId);
        }

        // Helper methods for testing




    }
}
