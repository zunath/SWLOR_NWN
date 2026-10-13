namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Attribution for damage that is decided now but applied later, by a deferred
    /// AssignCommand or by the engine's attack animation.
    /// </summary>
    internal struct CombatRecordPendingDamage
    {
        public uint Source;
        public uint Target;
        public int Amount;
        public bool IsWeaponHit;
        public ulong Frame;
        public long TimestampMs;
        public CombatRecordAttribution Attribution;
    }
}
