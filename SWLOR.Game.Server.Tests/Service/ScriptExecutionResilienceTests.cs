using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public sealed class ScriptExecutionResilienceTests
{
    [Test]
    public void ExecuteScript_ReportsBrokenHandlerAndContinuesWithRemainingHandlers()
    {
        var originalProvider = ScriptExecutionProvider.Current;
        var provider = new RecordingScriptExecutionProvider();
        ScriptExecutionProvider.SetProvider(provider);

        try
        {
            SWLOR.NWN.API.NWScript.NWScript.ExecuteScript("test_script", 42);
        }
        finally
        {
            ScriptExecutionProvider.SetProvider(originalProvider);
        }

        provider.SuccessfulHandlerRan.Should().BeTrue();
        provider.ReportedExceptions.Should().ContainSingle();
        provider.ReportedExceptions[0].ScriptName.Should().Be("test_script");
        provider.ReportedExceptions[0].HandlerName.Should().Be("BrokenHandler");
        provider.ReportedExceptions[0].Exception.Message.Should().Be("handler failed");
    }

    [Test]
    public void GuiCache_RunsInTheMainAfterCachePhase()
    {
        var cacheMethod = typeof(Gui).GetMethod(nameof(Gui.CacheData));

        cacheMethod.Should().NotBeNull();
        cacheMethod!.GetCustomAttributes(typeof(NWNEventHandler), false)
            .Cast<NWNEventHandler>()
            .Select(attribute => attribute.Script)
            .Should().ContainSingle()
            .Which.Should().Be(ScriptName.OnModuleCacheAfter);
    }

    private sealed class RecordingScriptExecutionProvider : IScriptExecutionProvider
    {
        public uint ObjectSelf { get; set; }
        public bool SuccessfulHandlerRan { get; private set; }
        public List<ReportedScriptException> ReportedExceptions { get; } = new();

        public bool HasScript(string scriptName) => scriptName == "test_script";

        public IEnumerable<(Action action, string name)> GetActionScripts(string scriptName)
        {
            yield return (() => throw new InvalidOperationException("handler failed"), "BrokenHandler");
            yield return (() => SuccessfulHandlerRan = true, "SuccessfulHandler");
        }

        public void ReportScriptException(string scriptName, string handlerName, Exception exception)
        {
            ReportedExceptions.Add(new ReportedScriptException(scriptName, handlerName, exception));
        }

        public void ExecuteInScriptContext(Action action, uint objectId = 0x7F000000, int scriptEventId = 0)
        {
            ObjectSelf = objectId;
            action();
        }
    }

    private sealed record ReportedScriptException(
        string ScriptName,
        string HandlerName,
        Exception Exception);
}
