using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The two <c>mod_preload</c> handlers that split <c>EventRegistration.OnModulePreload</c> (P4-8): <c>StartServerEvents</c>
/// signals APPLICATION_SHUTDOWN at process exit and starts the 6-second heartbeat (both live in the game build, so a hot
/// reload reruns it), then <c>OnModulePreload</c> does the rest of the old handler (boot only): the legacy item property
/// hook, the engine and NWNX event bindings, <c>mod_content_chg</c> and the cache scripts. The boot-only part installs a
/// native hook, so it cannot run without a server; these tests read the source instead, and
/// <c>ScriptHandlerOrderTests.BootBucketsAreRunInTheRecordedOrder</c> still pins the cache scripts it runs.
/// </summary>
[TestFixture]
public class EventRegistrationBootTests
{
    // What OnModulePreload and the methods it calls did before the split, and the two statements StartServerEvents took.
    private static readonly string[] BootOnlyMethods =
        { "OnModulePreload", "HookModuleEvents", "HookAreaEvents", "HookNWNXEvents", "HookApplicationEvents" };

    [Test]
    public void StartServerEvents_runs_before_OnModulePreload_and_a_hot_reload_reruns_only_it()
    {
        var manifest = ScriptHandlerManifest.LoadEmbedded(typeof(ScriptRegistry).Assembly);
        var preload = manifest.GetHandlers(ScriptName.OnModulePreload, false).ToList();
        var start = preload.IndexOf("SWLOR.Game.Server.Feature.EventRegistration.StartServerEvents");

        start.Should().BeGreaterThanOrEqualTo(0);
        preload[start + 1].Should().Be("SWLOR.Game.Server.Feature.EventRegistration.OnModulePreload");
        Phase(nameof(EventRegistration.StartServerEvents)).Should().Be(BootReload.Rerun);
        Phase(nameof(EventRegistration.OnModulePreload)).Should().Be(BootReload.BootOnly);
    }

    [Test]
    public void StartServerEvents_only_subscribes_the_shutdown_signal_and_starts_the_heartbeat()
    {
        var statements = Method("StartServerEvents").Body!.Statements.Select(statement => statement.NormalizeWhitespace().ToFullString()).ToList();

        statements.Should().HaveCount(2);
        var signal = statements[0];
        (signal.Contains("ProcessExit +=") && signal.Contains("EventsPlugin.SignalEvent(\"APPLICATION_SHUTDOWN\", GetModule())")
         || signal.Contains("SubscribeProcessExit(")).Should().BeTrue(
            "the first statement signals APPLICATION_SHUTDOWN when the process exits, as the old handler did: " + signal);
        statements[1].Should().StartWith("Scheduler.ScheduleRepeating(")
            .And.Contain("ExecuteScript(ScriptName.OnSwlorHeartbeat, GetModule())")
            .And.EndWith("TimeSpan.FromSeconds(6));");
    }

    [Test]
    public void The_boot_only_part_no_longer_starts_the_heartbeat_or_the_shutdown_signal_and_keeps_every_binding()
    {
        foreach (var name in BootOnlyMethods)
        {
            var body = Method(name).Body!.ToFullString();
            body.Should().NotContain("Scheduler.", $"{name} is boot only; the heartbeat moved to StartServerEvents");
            body.Should().NotContain("ProcessExit", $"{name} is boot only; the shutdown signal moved to StartServerEvents");
        }

        var preload = Method("OnModulePreload").Body!.ToFullString();
        preload.Should().Contain("Native.LegacyItemProperties.RegisterHook();")
            .And.Contain("HookModuleEvents();").And.Contain("HookAreaEvents();").And.Contain("HookNWNXEvents();")
            .And.Contain("HookApplicationEvents();").And.Contain("ExecuteScript(ScriptName.OnModuleContentChange, GetModule());");
        Method("HookApplicationEvents").Body!.ToFullString()
            .Should().Contain("EventsPlugin.SubscribeEvent(\"APPLICATION_SHUTDOWN\", ScriptName.OnSWLORApplicationShutdown);")
            .And.Contain("EventsPlugin.SubscribeEvent(\"SWLOR_ON_DAMAGE\", ScriptName.OnSWLORDamage);");

        var start = Method("StartServerEvents").Body!.ToFullString();
        foreach (var binding in new[] { "SetEventScript", "SubscribeEvent(", "RegisterHook", "DB.", "OnModuleCache", "OnModuleContentChange" })
            start.Should().NotContain(binding, "the part a hot reload reruns changes nothing that survives in the engine or Redis");
    }

    private static BootReload Phase(string method) =>
        ((BootPhaseAttribute)Attribute.GetCustomAttribute(typeof(EventRegistration).GetMethod(method)!, typeof(BootPhaseAttribute))!).Reload;

    private static MethodDeclarationSyntax Method(string name)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        var path = Path.Combine(directory!.FullName, "SWLOR.Game.Server", "Feature", "EventRegistration.cs");
        return CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == name);
    }
}
