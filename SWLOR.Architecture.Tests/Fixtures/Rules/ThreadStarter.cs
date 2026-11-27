namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Starts a dedicated thread.</summary>
public sealed class ThreadStarter
{
    public Thread Start()
    {
        var thread = new Thread(() => { });
        thread.Start();
        return thread;
    }
}
