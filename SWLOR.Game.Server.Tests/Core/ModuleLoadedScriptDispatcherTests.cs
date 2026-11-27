using FluentAssertions;
using Nwn.Hosting.Contracts.Generations;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The boot of a server with hot reload on takes its reload digests once the module's load script has run, every boot script
/// having run by then (<see cref="ModuleLoadedScriptDispatcher"/>).
/// </summary>
[TestFixture]
public class ModuleLoadedScriptDispatcherTests
{
    private sealed class Inner : IScriptDispatcher
    {
        public List<string> Scripts { get; } = new();

        public bool TryHandle(string scriptName, uint objectSelf, out bool conditionalResult)
        {
            Scripts.Add(scriptName);
            conditionalResult = scriptName == "cond_t";
            return scriptName != "unknown";
        }
    }

    [Test]
    public void Every_script_goes_to_the_server_and_the_module_load_script_calls_back_once_after_it_ran()
    {
        var inner = new Inner();
        var calls = new List<string>();
        var dispatcher = new ModuleLoadedScriptDispatcher(inner, () => calls.Add("loaded after " + string.Join(",", inner.Scripts)));

        dispatcher.TryHandle(ScriptName.OnModulePreload, 1, out _).Should().BeTrue();
        dispatcher.TryHandle("cond_t", 1, out var conditional).Should().BeTrue();
        dispatcher.TryHandle("unknown", 1, out _).Should().BeFalse();
        calls.Should().BeEmpty();
        dispatcher.TryHandle(ScriptName.OnModuleLoad, 1, out _).Should().BeTrue();
        dispatcher.TryHandle(ScriptName.OnModuleLoad, 1, out _).Should().BeTrue();

        conditional.Should().BeTrue();
        calls.Should().Equal($"loaded after {ScriptName.OnModulePreload},cond_t,unknown,{ScriptName.OnModuleLoad}");
    }

    [Test]
    public void It_needs_the_server_dispatcher_and_the_callback()
    {
        var withoutInner = () => new ModuleLoadedScriptDispatcher(null!, () => { });
        var withoutCallback = () => new ModuleLoadedScriptDispatcher(new Inner(), null!);

        withoutInner.Should().Throw<ArgumentNullException>();
        withoutCallback.Should().Throw<ArgumentNullException>();
    }
}
