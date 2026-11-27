namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Classifies a handler of <c>mod_enter</c> or <c>mod_exit</c> for hot reload (<see cref="PlayerReload"/>). A normal login or
    /// logout never reads it; the reload does (<see cref="PlayerReloadPlan"/>), only when a generation quiesces or activates with
    /// players online. A unit test fails when a handler of either script has none, and a snapshot records every decision.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class PlayerSessionAttribute : Attribute
    {
        public PlayerReload Reload { get; }

        /// <summary>Why the handler has this class: what it does and what a reload does instead.</summary>
        public string Reason { get; }

        public PlayerSessionAttribute(PlayerReload reload, string reason)
        {
            Reload = reload;
            Reason = reason;
        }

        /// <summary>
        /// For <see cref="PlayerReload.Restore"/> and <see cref="PlayerReload.Quiesce"/>: the name of a public method of the same class,
        /// static when the handler is, that takes the player (<c>uint</c>) and returns nothing. The handler is exactly a call of it
        /// with the entering or exiting object, so a login or logout runs the same work it always did, and the reload runs it for
        /// each online player.
        /// </summary>
        public string Step { get; set; }
    }
}
