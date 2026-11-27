using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks.Tests.Support;

namespace SWLOR.Game.NativeHooks.Tests;

/// <summary>
/// The trampolines run on the engine thread with no lock, lease or allocation (MaintainabilityRefactorHotReload.md,
/// package gap 5): a call costs a volatile read of the sink and one interface call, or the engine function's own call
/// when no sink is bound. Pinned for every hook, the per-frame ones (<c>ComputeUpdateRequired</c>,
/// <c>WriteGameObjUpdate_UpdateObject</c>, <c>GetMaxHitPoints</c>, <c>CalcStatModifier</c>) and the attack and damage hooks
/// among them, with sinks that allocate nothing themselves.
/// </summary>
[TestFixture]
public class TrampolineAllocationTests
{
    private const int Calls = 1000;

    private static readonly string[] HotPaths =
    [
        "ComputeUpdate", "WriteUpdate", "GetMaximum", "CalculateModifier", "ResolveAttackRoll", "GetDamageRoll", "AttackObject",
        "ApplyDamage", "ApplyHeal"
    ];

    private static IEnumerable<TestCaseData> EveryFunction() =>
        HookedFunctions.All.Select(function => new TestCaseData(function).SetArgDisplayNames(function.ToString()));

    [Test]
    public void The_hot_paths_are_among_the_pinned_functions()
    {
        HookedFunctions.All.Select(function => function.SinkMethod).Should().Contain(HotPaths);
    }

    [TestCaseSource(nameof(EveryFunction))]
    public void A_call_with_no_sink_bound_allocates_nothing(HookedFunction function)
    {
        Allocated(function).Should().Be(0);
    }

    [TestCaseSource(nameof(EveryFunction))]
    public void A_call_with_a_sink_bound_allocates_nothing(HookedFunction function)
    {
        var sinks = RecordingSinks.Create();
        HookHarness.Table.Bind(sinks);
        try
        {
            Allocated(function).Should().Be(0);
        }
        finally
        {
            HookHarness.Table.Unbind(sinks);
        }
    }

    private static long Allocated(HookedFunction function)
    {
        var hook = HookHarness.Functions.Hooks[function.EntryPoint];
        var invoke = function.Invoke;

        // Warm up: the first calls JIT the trampoline, the stand-in and the sink.
        for (var call = 0; call < 10; call++)
            invoke(hook);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var call = 0; call < Calls; call++)
            invoke(hook);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
