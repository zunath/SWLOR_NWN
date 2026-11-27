namespace SWLOR.Game.Server.Core
{
    /// <summary>When a <see cref="ReloadHookAttribute"/> method runs during a hot reload.</summary>
    public enum ReloadHookPhase
    {
        /// <summary>
        /// In the old build while it quiesces, before the players' quiescence steps: ends work in flight that a closure of the old
        /// build would have finished or released (a cast, an item use, a placed object's timed removal), as an interruption ends it.
        /// </summary>
        Quiesce = 0,

        /// <summary>
        /// In the new build once its carried state is back, before the players' restore steps: restarts what a dropped closure kept
        /// going (a polling loop, a timed clean-up) from the state the engine or the carried fields still hold.
        /// </summary>
        Resume = 1,
    }
}
