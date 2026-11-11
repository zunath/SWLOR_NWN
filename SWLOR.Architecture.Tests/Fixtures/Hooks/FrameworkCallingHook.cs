using System.Runtime.InteropServices;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks;

/// <summary>A native hook that does work of its own with a framework call before it forwards.</summary>
public static class FrameworkCallingHook
{
    private static IFixtureSink? _sink;

    [UnmanagedCallersOnly]
    public static int Forward(int value) => _sink!.Run(Math.Abs(value));
}
