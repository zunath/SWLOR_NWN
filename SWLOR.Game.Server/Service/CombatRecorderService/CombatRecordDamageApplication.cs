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

        /// <summary>
        /// HP left by the hit, captured before something inside the call changes it: a reactive
        /// heal, another hit, a fatal-damage save or a subdual resurrection.
        /// </summary>
        public bool HasHitPointsLeftByHit;
        public int HitPointsLeftByHit;

        /// <summary>The target's death in this call is recorded, whichever death handler ran first.</summary>
        public bool DeathRecorded;

        /// <summary>What temporary-HP pools had left when a reaction removed or replaced them.</summary>
        public CombatRecordTemporaryHitPointPool[] PoolsAtRemoval;
    }
}
