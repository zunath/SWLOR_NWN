namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Why a cast or channel stopped before it completed.
    /// </summary>
    public enum CombatRecordInterruptCause : byte
    {
        Other = 0,
        Moved = 1,

        /// <summary>Another creature's ability or effect stopped the cast.</summary>
        CrowdControl = 2
    }
}
