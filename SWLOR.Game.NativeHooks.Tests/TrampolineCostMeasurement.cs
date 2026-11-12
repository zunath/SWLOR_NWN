using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks.Tests.Support;

namespace SWLOR.Game.NativeHooks.Tests;

/// <summary>
/// Measurement, not a check (explicit): nanoseconds per call of the per-frame <c>GetMaxHitPoints</c> trampoline, bound and
/// unbound, against a copy of the trampoline the game assembly held before (P4-9 and P4-10: a plain static sink field,
/// no null check). The difference is the volatile read and the null check.
/// </summary>
[TestFixture]
[Explicit("Measurement; prints ns per call")]
public unsafe class TrampolineCostMeasurement
{
    private const int Calls = 20_000_000;

    private static IVitalityHitPointsSink _previousSink = null!;
    private static FunctionHook* _previousHook;

    /// <summary>The shape of the game's trampoline before this change.</summary>
    [UnmanagedCallersOnly]
    private static short PreviousGetMaximum(void* pointer, int includeToughness)
        => _previousSink.GetMaximum(pointer, includeToughness, (nint)_previousHook->m_trampoline);

    [Test]
    public void Measure_GetMaxHitPoints_trampoline_cost()
    {
        var function = HookedFunctions.All.Single(entry => entry.SinkMethod == "GetMaximum");
        var hook = HookHarness.Functions.Hooks[function.EntryPoint];
        var sinks = RecordingSinks.Create();
        _previousSink = sinks.VitalityHitPoints;
        _previousHook = (FunctionHook*)HookHarness.Functions.FunctionHooks[function.EntryPoint];
        delegate* unmanaged<void*, int, short> previous = &PreviousGetMaximum;
        var current = (delegate* unmanaged<void*, int, short>)hook;

        HookHarness.Table.Bind(sinks);
        try
        {
            for (var round = 0; round < 3; round++)
            {
                var before = Time(previous);
                var bound = Time(current);
                TestContext.Out.WriteLine($"round {round}: previous {before:F2} ns, bound {bound:F2} ns per call");
            }
        }
        finally
        {
            HookHarness.Table.Unbind(sinks);
        }

        TestContext.Out.WriteLine($"unbound (engine stand-in only) {Time(current):F2} ns per call");
    }

    private static double Time(delegate* unmanaged<void*, int, short> trampoline)
    {
        for (var call = 0; call < 100_000; call++)
            trampoline((void*)0x1000, 1);

        var watch = Stopwatch.StartNew();
        for (var call = 0; call < Calls; call++)
            trampoline((void*)0x1000, 1);
        watch.Stop();
        return watch.Elapsed.TotalMilliseconds * 1_000_000 / Calls;
    }
}
