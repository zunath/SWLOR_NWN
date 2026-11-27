using SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks.Sinks;

/// <summary>A sink implementation in the namespace the rule names as the sinks' home.</summary>
public sealed class HomedSink : IFixtureSink
{
    public int Run(int value) => value;
}
