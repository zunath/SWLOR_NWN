namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Damage amounts of one applied damage effect, grouped by SWLOR damage type.
    /// </summary>
    public struct CombatRecordDamageBreakdown
    {
        // The engine stores one amount per damage-type bit: bludgeoning, piercing, slashing,
        // magical, acid, cold, divine, electrical, fire, negative, positive, sonic, base weapon.
        public const int NativeDamageTypeCount = 13;

        public int Physical;
        public int Force;
        public int Fire;
        public int Poison;
        public int Electrical;
        public int Ice;
        public int Sonic;
        public int Other;

        public int Total => Physical + Force + Fire + Poison + Electrical + Ice + Sonic + Other;

        /// <summary>
        /// Adds an engine damage-effect amount by its native type index. Unused slots hold -1.
        /// </summary>
        public void AddNative(int nativeIndex, int amount)
        {
            if (amount <= 0)
                return;

            switch (nativeIndex)
            {
                case 0:
                case 1:
                case 2:
                case 12:
                    Physical += amount;
                    break;
                case 3:
                    Force += amount;
                    break;
                case 4:
                    Poison += amount;
                    break;
                case 5:
                    Ice += amount;
                    break;
                case 7:
                    Electrical += amount;
                    break;
                case 8:
                    Fire += amount;
                    break;
                case 11:
                    Sonic += amount;
                    break;
                default:
                    Other += amount;
                    break;
            }
        }
    }
}
