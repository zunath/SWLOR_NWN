using System.Runtime.InteropServices;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks;

/// <summary>A native hook that keeps part of its body in a helper of its own class.</summary>
public static class HelperCallingHook
{
    private static IFixtureSink? _sink;

    [UnmanagedCallersOnly]
    public static int Forward(int value) => _sink!.Run(Adjust(value));

    private static int Adjust(int value) => value + 1;
}
