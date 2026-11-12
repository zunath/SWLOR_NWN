using SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks.Elsewhere;

/// <summary>A sink implementation outside the namespace the rule names as the sinks' home.</summary>
public sealed class MisplacedSink : IFixtureSink
{
    public int Run(int value) => value;
}
