namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Subscribes to the task scheduler's unobserved-exception event.</summary>
public sealed class UnobservedTaskSubscriber
{
    public void Hook() => TaskScheduler.UnobservedTaskException += (_, _) => { };
}
