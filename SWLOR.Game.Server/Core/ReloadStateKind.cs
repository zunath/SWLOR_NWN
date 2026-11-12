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
        /// State the game changes as it runs (per player, per frame, timers), or the working state of a boot-only handler that
        /// nothing reads afterwards. Neither a rerun nor an adoption rebuilds it, so the cache digest leaves it out.
        /// </summary>
        Runtime = 1,

        /// <summary>
        /// A cache a rerun handler builds with random rolls as it loads (the special fish of the fishing spots, the random respawn
        /// delays of some spawn tables). A reload rolls it again, as a restart does: carrying the old roll would also hide the
        /// reloaded code's changes to the same cache. The cache digest leaves it out.
        /// </summary>
        Rerolled = 2,
    }
}
