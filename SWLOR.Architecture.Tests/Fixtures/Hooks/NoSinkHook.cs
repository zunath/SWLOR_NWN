using System.Runtime.InteropServices;

namespace SWLOR.Architecture.Tests.Fixtures.Hooks;

/// <summary>A native hook that has no sink at all.</summary>
public static class NoSinkHook
{
    [UnmanagedCallersOnly]
    public static int Forward(int value) => value;
}
