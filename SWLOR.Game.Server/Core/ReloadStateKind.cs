namespace SWLOR.Game.Server.Core
{
    /// <summary>How a hot reload treats a field of a service that owns boot or reload state (<see cref="ReloadStateAttribute"/>).</summary>
    public enum ReloadStateKind
    {
        /// <summary>
        /// Rebuilt by an adoption step from the live world, Redis or the carried state, not by a rerun handler. Its reload digest
        /// is compared with the old build's at quiescence, not with the boot's.
        /// </summary>
        Adopted = 0,

        /// <summary>
        /// State the game changes as it runs (per player, per frame, timers). Neither a rerun nor an adoption rebuilds it, so the
        /// cache digest leaves it out.
        /// </summary>
        Runtime = 1,
    }
}
