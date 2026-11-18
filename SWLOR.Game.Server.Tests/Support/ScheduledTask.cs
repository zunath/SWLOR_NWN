#nullable enable

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class ScheduledTask : IDisposable
{
    public ScheduledTask(Action action) => Action = action;
    public Action Action { get; }
    public bool Disposed { get; private set; }
    public void Dispose() => Disposed = true;
}
