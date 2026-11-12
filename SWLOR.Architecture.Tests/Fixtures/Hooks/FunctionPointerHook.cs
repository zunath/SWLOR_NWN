using System.Runtime.InteropServices;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks;

/// <summary>A native hook that calls the engine function through a function pointer itself instead of leaving that to its sink.</summary>
public static unsafe class FunctionPointerHook
{
    private static IFixtureSink? _sink;
    private static delegate* unmanaged<int, int> _original;

    [UnmanagedCallersOnly]
    public static int Forward(int value) => _original(_sink!.Run(value));
}
