namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// A damage effect the engine is applying right now. Lethal damage runs the death scripts
    /// inside that call, so a death of <see cref="Target"/> is held here and recorded after the
    /// damage event that caused it.
    /// </summary>
    internal struct CombatRecordDamageApplication
    {
        public uint Target;
        public bool TargetDied;
        public uint Killer;
    }
}
