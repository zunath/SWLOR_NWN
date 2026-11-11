namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Lets go of everything a running server holds in the process, so a build can be retired. Generations call it
    /// later (P4-13); nothing calls it on a reload yet.
    /// </summary>
    public interface IServerTeardown
    {
        /// <summary>
        /// Runs every step, in order: cancel the scheduler timers and queue, drop the <c>NwTask</c> queue, run the
        /// <c>app_shutdown</c> clean-up (combat recorder files closed, then logs flushed), remove the process-wide event
        /// subscriptions, clear the script execution provider. A step that throws does not stop the later ones; the
        /// exceptions are rethrown together afterwards.
        /// </summary>
        void Run();
    }
}
