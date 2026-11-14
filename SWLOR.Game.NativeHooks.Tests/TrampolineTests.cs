using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks.Tests.Support;

namespace SWLOR.Game.NativeHooks.Tests;

/// <summary>
/// What each of the 36 trampolines does: with no sink bound it runs the engine function with the arguments it was called
/// with and returns its result; with a sink bound it forwards the same arguments and the engine function to the sink
/// (the three hooks whose sink replaces the engine function pass none) and returns the sink's result; unbinding returns it
/// to the engine function.
/// </summary>
[TestFixture]
public class TrampolineTests
{
    private NativeSinkSet? _bound;

    private static IEnumerable<TestCaseData> EveryFunction() =>
        HookedFunctions.All.Select(function => new TestCaseData(function).SetArgDisplayNames(function.ToString()));

    [SetUp]
    public void Reset() => CallLog.Reset();

    [TearDown]
    public void UnbindTheTestsSinks()
    {
        if (_bound != null)
            HookHarness.Table.Unbind(_bound);
        _bound = null;
    }

    private NativeSinkSet Bind()
    {
        _bound = RecordingSinks.Create();
        HookHarness.Table.Bind(_bound);
        return _bound;
    }

    [TestCaseSource(nameof(EveryFunction))]
    public void With_no_sink_bound_the_hook_runs_the_engine_function_with_the_same_arguments(HookedFunction function)
    {
        var result = function.Invoke(HookHarness.Functions.Hooks[function.EntryPoint]);

        CallLog.Calls.Should().Be(1);
        CallLog.Original.Should().Be(function.EntryPoint, "the hook runs its own engine function");
        CallLog.SinkMethod.Should().BeNull();
        CallLog.RecordedArgs().Should().Equal(function.Args);
        result.Should().Be(function.EngineResult);
    }

    [TestCaseSource(nameof(EveryFunction))]
    public void With_a_sink_bound_the_hook_forwards_to_it_with_the_engine_function_and_never_runs_it(HookedFunction function)
    {
        Bind();

        var result = function.Invoke(HookHarness.Functions.Hooks[function.EntryPoint]);

        CallLog.Calls.Should().Be(1);
        CallLog.Original.Should().Be(-1, "the sink decides whether the engine function runs");
        CallLog.SinkMethod.Should().Be(function.SinkMethod);
        CallLog.SinkOriginal.Should().Be(function.SinkGetsOriginal ? HookHarness.Functions.StandIn(function.EntryPoint) : 0,
            "the sink gets the trampoline of its own engine function");
        CallLog.RecordedArgs().Should().Equal(function.Args);
        result.Should().Be(function.SinkResult);
    }

    [TestCaseSource(nameof(EveryFunction))]
    public void After_unbinding_the_hook_runs_the_engine_function_again(HookedFunction function)
    {
        var sinks = Bind();
        HookHarness.Table.Unbind(sinks);
        _bound = null;

        var result = function.Invoke(HookHarness.Functions.Hooks[function.EntryPoint]);

        CallLog.Original.Should().Be(function.EntryPoint);
        CallLog.SinkMethod.Should().BeNull("no hook references the unbound build");
        result.Should().Be(function.EngineResult);
    }

    [Test]
    public void Unbinding_a_set_that_is_not_bound_leaves_the_bound_sinks_in_place()
    {
        Bind();

        HookHarness.Table.Unbind(RecordingSinks.Create());
        var function = HookedFunctions.All.Single(entry => entry.SinkMethod == "GetMaximum");
        function.Invoke(HookHarness.Functions.Hooks[function.EntryPoint]);

        CallLog.SinkMethod.Should().Be("GetMaximum", "a retired build's unbind must not unbind the active build");
    }

    [Test]
    public void Binding_replaces_the_bound_sinks_and_unbinding_clears_every_hook()
    {
        var first = Bind();
        var second = RecordingSinks.Create();
        HookHarness.Table.Bind(second);
        _bound = second;

        HookHarness.Table.Unbind(first);
        foreach (var function in HookedFunctions.All)
        {
            CallLog.Reset();
            function.Invoke(HookHarness.Functions.Hooks[function.EntryPoint]);
            CallLog.SinkMethod.Should().Be(function.SinkMethod, $"{function} still forwards to the second set");
        }

        HookHarness.Table.Unbind(second);
        _bound = null;
        foreach (var function in HookedFunctions.All)
        {
            CallLog.Reset();
            function.Invoke(HookHarness.Functions.Hooks[function.EntryPoint]);
            CallLog.Original.Should().Be(function.EntryPoint, $"{function} runs the engine function once nothing is bound");
        }
    }
}
