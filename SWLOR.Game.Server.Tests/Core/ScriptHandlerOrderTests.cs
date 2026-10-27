using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Records the order the server runs its boot handlers and script handlers in, and fails when it changes.
///
/// <para><b>How the order is produced.</b> Each script runs its handlers front to back (one failing action handler is
/// logged and the rest still run; every conditional handler runs and the results are ANDed). The order is declared by
/// the script handler manifest, <c>SWLOR.Game.Server/Core/ScriptHandlerOrder.txt</c>, which the server embeds and
/// <c>ScriptRegistry</c> reads at boot (see <see cref="ScriptHandlerManifest"/>). A handler the manifest does not list
/// runs after the listed ones, in discovery order, and a listed handler that no longer exists is ignored. The boot
/// buckets are ordinary scripts: <c>mod_preload</c> and <c>mod_load</c> are run by the engine, and
/// <c>EventRegistration.OnModulePreload</c> runs <c>mod_content_chg</c> (only when the module file changed),
/// <c>mod_cache_bef</c> and <c>mod_cache_aft</c> in that order.</para>
///
/// <para><b>Why the manifest exists.</b> Before it, the order was whatever reflection returned
/// (<c>GetTypes</c>, then <c>GetMethods</c>, then the attributes), which follows the compiler's type emission order:
/// namespaces depth first, a namespace's own types before its child namespaces, siblings last-declared-first, the
/// project's .cs files sorted by full path. Adding a file that had no handler at all could reorder every handler: a
/// throwaway <c>Core/AAA_Unrelated.cs</c> that only declared <c>namespace SWLOR.Game.Server.Service</c> moved
/// <c>Service.DB.Load</c> after <c>Feature.EventRegistration.OnModulePreload</c> in <c>mod_preload</c>, although
/// <c>DB.Load</c> (which connects Redis) must run before <c>OnModulePreload</c> (which reads it). Moving a handler class
/// to another folder or namespace, renaming a folder, and splitting or merging partial parts had the same effect.
/// <see cref="ScriptHandlerManifestOrderTests"/> proves the manifest ends that dependence.</para>
///
/// <para><b>What a failure here means.</b> The manifest and the registry disagree: a handler was added, renamed, moved
/// to another type or removed without the manifest following. The fix is to regenerate the manifest (the command is in
/// the failure message, then rebuild) and review the diff; a new handler is appended to the end of its script, so move
/// its line if it must run earlier. This test never judges whether the order is right, only that the manifest is complete.</para>
///
/// <para>Which assemblies are scanned, and in what order, is load order. On a production server only
/// <c>SWLOR.Game.Server</c> holds handlers. When engine tests are enabled <c>SWLOR.Game.Server.EngineTests</c> is
/// loaded before the scan; its handlers are not in the game's manifest, so they run after the game's for the same
/// script whatever the assembly order is (<see cref="EngineTestHandlersRunAfterEveryGameHandlerOfTheSameScript"/>), and
/// their own snapshot records their order.</para>
///
/// <para>The tests scan <c>SWLOR.Game.Server</c> alone, not the test host's assemblies, through
/// <c>ScriptRegistry.LoadHandlersFromAssemblies</c>; <see cref="ScriptHandlerSeamTests"/> shows that the entry point
/// the server calls does the same discovery.</para>
/// </summary>
[TestFixture]
public class ScriptHandlerOrderTests
{
    private const string Manifest = "SWLOR.Game.Server/Core/ScriptHandlerOrder.txt";
    private const string EngineTestSnapshot = "SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.EngineTests.txt";
    private const string RegenerateCommand =
        "SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Game.Server.Tests --no-build --filter \"FullyQualifiedName~ScriptHandlerOrderTests\"" +
        "   (PowerShell: $env:SWLOR_UPDATE_SNAPSHOTS='1'; dotnet test ...; Remove-Item Env:SWLOR_UPDATE_SNAPSHOTS)" +
        "   then rebuild SWLOR.Game.Server.Tests so the server assembly embeds the new manifest";

    private static Assembly GameAssembly => typeof(ScriptRegistry).Assembly;

    private static Assembly EngineTestAssembly => Assembly.Load(new AssemblyName("SWLOR.Game.Server.EngineTests"));

    [Test]
    public void ProductionHandlerOrderMatchesTheManifest()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly });

        var text = ScriptHandlerOrderSnapshot.Render(
            "SWLOR script handler order manifest: SWLOR.Game.Server only (the order a production server runs handlers in; engine tests disabled)",
            registry);

        SnapshotComparison.AssertMatches(Manifest, text, RegenerateCommand);
    }

    [Test]
    public void ManifestListsEveryGameHandlerAndNothingElse()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly });

        registry.UnlistedHandlers.Should().BeEmpty(
            "every [NWNEventHandler] method must be listed in SWLOR.Game.Server/Core/ScriptHandlerOrder.txt; regenerate it: " + RegenerateCommand);
        registry.StaleManifestEntries.Should().BeEmpty(
            "every line of SWLOR.Game.Server/Core/ScriptHandlerOrder.txt must name an existing handler; regenerate it: " + RegenerateCommand);
    }

    [Test]
    public void EngineTestHandlerOrderMatchesTheSnapshot()
    {
        var engineTests = EngineTestAssembly;
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly, engineTests });

        var snapshot = ScriptHandlerOrderSnapshot.Render(
            "SWLOR script handler order: handlers added by SWLOR.Game.Server.EngineTests (a server with engine tests enabled). " +
            "They run after the game's handlers of the same script.",
            registry,
            method => method.DeclaringType!.Assembly == engineTests);

        SnapshotComparison.AssertMatches(EngineTestSnapshot, snapshot, RegenerateCommand);
    }

    [Test]
    public void EngineTestHandlersRunAfterEveryGameHandlerOfTheSameScript()
    {
        var gameOnly = new ScriptRegistry();
        gameOnly.LoadHandlersFromAssemblies(new[] { GameAssembly });
        var engineTests = EngineTestAssembly;

        // Both assembly orders: the manifest, not the order the assemblies are scanned in, keeps the game first.
        foreach (var assemblies in new[] { new[] { GameAssembly, engineTests }, new[] { engineTests, GameAssembly } })
        {
            var both = new ScriptRegistry();
            both.LoadHandlersFromAssemblies(assemblies);

            foreach (var script in ScriptHandlerOrderSnapshot.AllScriptNames(both))
            {
                foreach (var kind in new[] { "action", "conditional" })
                {
                    var game = ScriptHandlerOrderSnapshot.HandlerLines(gameOnly, script)
                        .Where(line => line.StartsWith(kind, StringComparison.Ordinal)).ToArray();
                    var all = ScriptHandlerOrderSnapshot.HandlerLines(both, script)
                        .Where(line => line.StartsWith(kind, StringComparison.Ordinal)).ToArray();

                    all.Take(game.Length).Should().Equal(game,
                        $"engine-test handlers of '{script}' must be listed after the game's {kind} handlers");
                }
            }
        }
    }

    [Test]
    public void BootBucketsAreRunInTheRecordedOrder()
    {
        // The three scripts EventRegistration.OnModulePreload runs itself are not discoverable by reflection, so
        // read the order from the method. mod_preload and mod_load are run by the engine.
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "EventRegistration.cs"));
        var method = Regex.Match(
            source,
            @"\[NWNEventHandler\(ScriptName\.OnModulePreload\)\]\s*public static void OnModulePreload\(\)\s*\{(?<body>.*?)\n        \}",
            RegexOptions.Singleline);
        method.Success.Should().BeTrue("EventRegistration.OnModulePreload is where the boot buckets are started");
        var body = method.Groups["body"].Value;

        var executed = Regex.Matches(body, @"ExecuteScript\(ScriptName\.(?<name>\w+),")
            .Select(m => (string)typeof(ScriptName).GetField(m.Groups["name"].Value)!.GetRawConstantValue()!)
            .ToArray();
        var recorded = ScriptHandlerOrderSnapshot.BootBuckets.Select(b => b.Script).ToArray();

        executed.Should().Equal(
            new[] { recorded[1], recorded[2], recorded[3] },
            "OnModulePreload must run mod_content_chg, mod_cache_bef and mod_cache_aft in this order; " +
            "update ScriptHandlerOrderSnapshot.BootBuckets and regenerate the manifest if that is intended");

        body.IndexOf("HookModuleEvents();", StringComparison.Ordinal)
            .Should().BeLessThan(body.IndexOf("ExecuteScript(", StringComparison.Ordinal),
                "module events (including mod_load) are hooked before any cache bucket runs");
        source.Should().Contain(
            "SetEventScript(module, EventScript.Module_OnModuleLoad, ScriptName.OnModuleLoad);",
            "mod_load runs because OnModulePreload points the module's OnModuleLoad event at it");
        recorded[0].Should().Be(ScriptName.OnModulePreload);
        recorded[4].Should().Be(ScriptName.OnModuleLoad);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
