namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Subscribes a named handler to the AppDomain's unhandled-exception event.</summary>
public sealed class UnhandledExceptionSubscriber
{
    public void Hook() => AppDomain.CurrentDomain.UnhandledException += OnUnhandled;

    private static void OnUnhandled(object sender, UnhandledExceptionEventArgs args)
    {
    }
}
