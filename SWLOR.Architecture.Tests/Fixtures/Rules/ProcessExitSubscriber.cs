namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Subscribes to a process-wide event of the AppDomain with a lambda no one can remove.</summary>
public sealed class ProcessExitSubscriber
{
    public void Hook() => AppDomain.CurrentDomain.ProcessExit += (_, _) => { };
}
