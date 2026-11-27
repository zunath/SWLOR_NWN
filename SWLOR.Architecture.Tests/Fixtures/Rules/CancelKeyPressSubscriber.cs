namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Subscribes to the console's cancel key.</summary>
public sealed class CancelKeyPressSubscriber
{
    public void Hook() => Console.CancelKeyPress += (_, _) => { };
}
