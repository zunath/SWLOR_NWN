namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Starts timers of the runtime that tick on their own thread.</summary>
public sealed class TimerStarter
{
    public IDisposable[] Start()
    {
        return
        [
            new Timer(_ => { }, null, 1000, 1000),
            new System.Timers.Timer(1000)
        ];
    }
}
