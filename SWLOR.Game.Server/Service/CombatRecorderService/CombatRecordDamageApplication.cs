namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// A damage effect the engine is applying right now. Reactions and death scripts run inside
    /// that call, so the events they raise are held from <see cref="HeldEventStart"/> on and
    /// written after the damage event that caused them.
    /// </summary>
    internal struct CombatRecordDamageApplication
    {
        public uint Target;
        public int HeldEventStart;
        public long StartedMs;

        /// <summary>HP left by the hit, captured before a subdual resurrection can restore it.</summary>
        public bool HasHitPointsAtDeath;
        public int HitPointsAtDeath;

        /// <summary>What temporary-HP pools had left when a reaction removed or replaced them.</summary>
        public CombatRecordTemporaryHitPointPool[] PoolsAtRemoval;
    }
}
