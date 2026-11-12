namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The process-wide state a running server leaves behind that <see cref="ServerTeardown"/> lets go of: the scheduler,
    /// the main-thread task queue, the log files, the script execution provider and the framework's type caches. One
    /// member per step so a test can observe each.
    /// </summary>
    public interface IServerStaticState
    {
        /// <summary>Cancels every <see cref="Scheduler"/> timer and queued item.</summary>
        void ClearScheduler();

        /// <summary>Drops the work queued on the <see cref="Async.NwTask"/> main-thread context.</summary>
        void ClearNwTaskQueue();

        /// <summary>Flushes and closes the log files (the <c>app_shutdown</c> step of the logger).</summary>
        void FlushLogs();

        /// <summary>Clears the script execution provider the server registered at boot.</summary>
        void ClearScriptExecutionProvider();

        /// <summary>
        /// Removes the build's types from the process-wide caches of System.ComponentModel (<see cref="TypeDescriptorCaches"/>),
        /// which would otherwise keep a retired build loaded.
        /// </summary>
        void ReleaseFrameworkTypeCaches();
    }
}
