namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// What a hot reload does with a handler of <c>mod_enter</c> (a player logs in) or <c>mod_exit</c> (a player logs out) for the
    /// players who are online while it runs (MaintainabilityRefactorHotReload.md, P4-15). The player stays connected, so the engine
    /// keeps their creature, effects, locals, HP, position and inventory, and Redis keeps their saved data; only the old build's
    /// managed memory goes. <see cref="PlayerSessionAttribute"/> records the decision; a normal login or logout never reads it.
    /// </summary>
    public enum PlayerReload
    {
        /// <summary>
        /// A <c>mod_enter</c> handler whose work the new build needs for a player who is already online: it rebuilds managed state
        /// of the player (or timers and closures of the old build) from the engine, Redis or the carried state, and has no effect a
        /// reload must not have. The reload runs its per-player step (<see cref="PlayerSessionAttribute.Step"/>) for every online
        /// player when the new build activates, in manifest order, before the first frame.
        /// </summary>
        Restore = 0,

        /// <summary>
        /// A <c>mod_enter</c> handler a reload never runs: what it did stays in the engine or Redis, its managed state is carried by
        /// a field carry instead (<see cref="ReloadStateKind.Carried"/>), or it is an effect of a login (a message, a teleport, an
        /// item or a count, a check or recovery for a freshly loaded character) that must not repeat for a player who stays online.
        /// </summary>
        LoginOnly = 1,

        /// <summary>
        /// A <c>mod_exit</c> handler whose work must happen before the old build goes: it saves state only the old build holds, or
        /// ends an activity whose managed state cannot be carried and would otherwise leave the player stuck (a busy flag, an
        /// immobilize effect, a swapped hotbar, a placeable without an owner). The reload runs its per-player step
        /// (<see cref="PlayerSessionAttribute.Step"/>) for every online player while the old build quiesces, in manifest order.
        /// </summary>
        Quiesce = 2,

        /// <summary>
        /// A <c>mod_exit</c> handler a reload never runs: it only forgets the player in the old build's memory (which goes with the
        /// build), its state is carried instead, or it is an effect of a logout (others lose the player's auras or party, a
        /// companion is despawned, a log line) that must not happen to a player who stays online.
        /// </summary>
        LogoutOnly = 3,
    }
}
