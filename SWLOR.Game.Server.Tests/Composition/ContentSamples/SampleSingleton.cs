using System.Threading;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>
/// A content singleton of the test containers (registered as its own class), for the factory and carry tests. Each instance has a
/// number of its own, so two containers' singletons differ by content as well as identity.
/// </summary>
public sealed class SampleSingleton
{
    private static int _created;

    public int Number { get; } = Interlocked.Increment(ref _created);
}
