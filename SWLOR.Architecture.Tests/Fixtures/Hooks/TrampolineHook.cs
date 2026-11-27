using System.Runtime.InteropServices;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks;

/// <summary>A trampoline as the rule wants it: the bound sink from a volatile field, or the engine function when none is bound.</summary>
public static unsafe class TrampolineHook
{
    private static volatile IFixtureSink? _sink;
    private static delegate* unmanaged<int, int> _original;

    [UnmanagedCallersOnly]
    public static int Forward(int value)
    {
        var sink = _sink;
        return sink is null ? _original(value) : sink.Run(value);
    }
}
