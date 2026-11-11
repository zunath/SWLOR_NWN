namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The process-wide events the server listens to. The one implementation, <see cref="AppDomainProcessEventSource"/>,
    /// is the only place in the server that subscribes to the process (an architecture rule); tests pass their own.
    /// </summary>
    public interface IProcessEventSource
    {
        event UnhandledExceptionEventHandler UnhandledException;
        event EventHandler ProcessExit;
    }
}
