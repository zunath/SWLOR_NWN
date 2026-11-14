using System.Runtime.InteropServices;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks;

/// <summary>A native hook that calls its sink and the engine function, but reads the sink from a plain field, not a volatile one.</summary>
public static unsafe class FunctionPointerHook
{
    private static IFixtureSink? _sink;
    private static delegate* unmanaged<int, int> _original;

    [UnmanagedCallersOnly]
    public static int Forward(int value) => _original(_sink!.Run(value));
}
