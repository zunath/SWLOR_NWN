namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// One native temporary-HP effect's remaining amount, captured around a damage application.
    /// </summary>
    public struct CombatRecordTemporaryHitPointPool
    {
        public ulong EffectId;
        public string EffectKey;
        public int Amount;
    }
}
