using System.Runtime.InteropServices;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks;

/// <summary>A native hook that only forwards to its sink.</summary>
public static class ForwardingHook
{
    private static IFixtureSink? _sink;

    [UnmanagedCallersOnly]
    public static int Forward(int value) => _sink!.Run(value);
}
