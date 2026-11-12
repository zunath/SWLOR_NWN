using FluentAssertions;
using NUnit.Framework;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Host.Bootstrap;
using SWLOR.Game.Host.Hooks;
using SWLOR.Game.NativeHooks;

namespace SWLOR.Game.Host.Tests.Hooks;

/// <summary>
/// The host's native hook extensions: when the native host starts, each finds the engine functions of its hook file and
/// requests no hook, so the game still installs every hook at its own boot moment.
/// </summary>
[TestFixture]
public unsafe class NativeHookExtensionTests
{
    private sealed class RecordingFunctions : INativeFunctions
    {
        public List<string> Exports { get; } = [];
        public int Requests { get; private set; }

        public nint GetExport(string symbol)
        {
            Exports.Add(symbol);
            return 0x1000 + Exports.Count;
        }

        public FunctionHook* RequestHook(nint function, nint hook, int order)
        {
            Requests++;
            throw new InvalidOperationException("an extension must not request a hook");
        }
    }

    [Test]
    public void Installing_every_extension_resolves_the_36_engine_functions_and_requests_no_hook()
    {
        var functions = new RecordingFunctions();
        var options = HostOptions.Create(_ => throw new InvalidOperationException("not created here"), new NativeHookTable(functions));

        foreach (var extension in options.Extensions)
            extension.Install();

        functions.Requests.Should().Be(0, "the game enables each hook from its own handler");
        functions.Exports.Should().HaveCount(36).And.OnlyHaveUniqueItems();
    }

    [Test]
    public void An_extension_stands_for_one_hook_file_and_needs_the_hooks()
    {
        var create = () => new NativeHookExtension(NativeHookId.VitalityHitPoints, null!);

        new NativeHookExtension(NativeHookId.VitalityHitPoints, new NativeHookTable()).Hook.Should().Be(NativeHookId.VitalityHitPoints);
        create.Should().Throw<ArgumentNullException>();
    }
}
