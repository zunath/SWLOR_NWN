namespace SWLOR.Game.Server.Core
{
    /// <summary>The current <see cref="AppDomain"/>'s process events. The only type that subscribes to them.</summary>
    public sealed class AppDomainProcessEventSource : IProcessEventSource
    {
        public event UnhandledExceptionEventHandler UnhandledException
        {
            add => AppDomain.CurrentDomain.UnhandledException += value;
            remove => AppDomain.CurrentDomain.UnhandledException -= value;
        }

        public event EventHandler ProcessExit
        {
            add => AppDomain.CurrentDomain.ProcessExit += value;
            remove => AppDomain.CurrentDomain.ProcessExit -= value;
        }
    }
}
