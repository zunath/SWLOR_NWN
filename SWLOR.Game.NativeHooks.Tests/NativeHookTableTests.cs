using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks.Tests.Support;

namespace SWLOR.Game.NativeHooks.Tests;

/// <summary>
/// Installing the hooks: resolving requests nothing, enabling requests every hook once with the symbols and hook orders
/// the game's hook files always used, and enabling again installs nothing more.
/// </summary>
[TestFixture]
public class NativeHookTableTests
{
    [Test]
    public void Resolving_finds_the_engine_functions_and_requests_no_hook()
    {
        var resolvedByEnable = HookedFunctions.All.Count(function => function.Hook == HookHarness.ResolvedByEnable);

        HookHarness.AfterResolve.Requests.Should().Be(0, "the host's extensions find the functions; only the game enables a hook");
        HookHarness.AfterResolve.Exports.Should().Be(HookedFunctions.All.Length - resolvedByEnable);
    }

    [Test]
    public void Enabling_a_hook_that_was_not_resolved_finds_its_engine_functions_itself()
    {
        HookHarness.ExportsAfterEnable.Should().Be(HookedFunctions.All.Length, "the native entry enables hooks no extension resolved");
        HookHarness.Functions.Exports.Should().OnlyHaveUniqueItems("a symbol is looked up once");
    }

    [Test]
    public void Enabling_requests_every_hook_once_with_the_symbols_and_orders_the_game_always_used()
    {
        HookHarness.Functions.Requests.Select(request => (request.Symbol, request.Order))
            .Should().Equal(HookedFunctions.All.Select(function => (function.Symbol, function.Order)));
    }

    [Test]
    public void Each_engine_function_gets_its_own_trampoline()
    {
        HookHarness.Functions.Hooks.Should().OnlyHaveUniqueItems().And.NotContain(0);
    }

    [Test]
    public void Enabling_again_installs_nothing_more()
    {
        var requests = HookHarness.Functions.Requests.Count;
        var exports = HookHarness.Functions.Exports.Count;

        foreach (var hook in Enum.GetValues<NativeHookId>())
        {
            HookHarness.Table.Enable(hook);
            HookHarness.Table.Enable(hook);
            HookHarness.Table.Resolve(hook);
        }

        HookHarness.Functions.Requests.Should().HaveCount(requests);
        HookHarness.Functions.Exports.Should().HaveCount(exports);
    }

    [Test]
    public void Every_hook_file_reports_itself_enabled_and_every_view_of_the_table_sees_it()
    {
        var anotherView = new NativeHookTable();

        foreach (var hook in Enum.GetValues<NativeHookId>())
        {
            HookHarness.Table.IsEnabled(hook).Should().BeTrue(hook.ToString());
            anotherView.IsEnabled(hook).Should().BeTrue("the hooks are the process's, whichever table asks: " + hook);
        }
    }

    [Test]
    public void The_17_hook_files_hook_the_36_engine_functions()
    {
        Enum.GetValues<NativeHookId>().Should().HaveCount(17);
        HookedFunctions.All.Should().HaveCount(36);
        HookedFunctions.All.Select(function => function.Hook).Distinct().Should().BeEquivalentTo(Enum.GetValues<NativeHookId>());
    }

    [Test]
    public void An_unknown_hook_is_refused()
    {
        var unknown = (NativeHookId)99;

        ((Action)(() => HookHarness.Table.Enable(unknown))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => HookHarness.Table.Resolve(unknown))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => HookHarness.Table.IsEnabled(unknown))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Binding_needs_a_set_of_sinks()
    {
        ((Action)(() => HookHarness.Table.Bind(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => HookHarness.Table.Unbind(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new NativeHookTable(null!))).Should().Throw<ArgumentNullException>();
    }
}
