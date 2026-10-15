namespace SWLOR.Game.Server.Service.CombatRecorderService
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
