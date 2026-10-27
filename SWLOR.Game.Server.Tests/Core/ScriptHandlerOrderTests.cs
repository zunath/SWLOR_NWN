using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Records the order the server runs its boot handlers and script handlers in today, and fails when it changes.
///
/// <para><b>How the order is produced.</b> At boot <c>ScriptRegistry.LoadHandlersFromAssembly</c> walks every loaded
/// assembly, every type (<c>GetTypes</c>), every public method (<c>GetMethods</c>) and every <c>[NWNEventHandler]</c>
/// attribute on it, and appends the method to the list of its script name. Nothing sorts. Each script runs its list
/// front to back (one failing action handler is logged and the rest still run; every conditional handler runs and the
/// results are ANDed). The boot buckets are ordinary scripts: <c>mod_preload</c> and <c>mod_load</c> are run by the
/// engine, and <c>EventRegistration.OnModulePreload</c> runs <c>mod_content_chg</c> (only when the module file changed),
/// <c>mod_cache_bef</c> and <c>mod_cache_aft</c> in that order.</para>
///
/// <para><b>Is the order deterministic?</b> For one set of source files, yes: rebuilding gives the same order every
/// time, because reflection returns metadata order and the compiler emits metadata deterministically. It is not stable
/// when files change, and nothing in the code declares it. Measured on this repository:</para>
/// <list type="bullet">
/// <item>Types come out in compile order. The compiler visits namespaces depth first, a namespace's own types (in file
/// order) before its child namespaces, and siblings last-declared-first: the namespace whose first file is latest in the
/// compile order is visited first. The compile order is MSBuild's wildcard order: the project's .cs files sorted by
/// full path, ordinal and ignoring case (checked against the real item lists on Windows and Linux). Methods inside a
/// type follow declaration order (a partial type follows the file order of its parts).</item>
/// <item>The order therefore depends on namespace names and file paths, not only on handler code. Adding a file that has
/// no handler at all can reorder every handler: a throwaway <c>Core/AAA_Unrelated.cs</c> that only declared
/// <c>namespace SWLOR.Game.Server.Service</c> moved <c>Service.DB.Load</c> after
/// <c>Feature.EventRegistration.OnModulePreload</c> in <c>mod_preload</c> and moved all of <c>Feature</c>'s
/// <c>mod_cache_bef</c> handlers ahead of <c>Service</c>'s. Today <c>DB.Load</c> (which connects Redis) runs before
/// <c>OnModulePreload</c> (which reads it) only because the <c>Service</c> namespace is visited before <c>Feature</c>.</item>
/// <item>The same applies to moving a handler class to another folder or namespace, renaming a folder, adding a
/// namespace, and splitting or merging partial parts. Adding a handler method to an existing type, or editing a handler's
/// body, does not move any other handler.</item>
/// <item>Windows and Linux give the same order. The snapshot was generated on Windows and these tests pass unchanged on
/// Linux (SDK 10.0.402 container, copy of the tree). The compile order differs between the two for one path pattern: a
/// file whose name starts with a sibling folder's name (<c>Leadership/</c> beside <c>LeadershipAbilityEffects.cs</c>)
/// sorts on either side of the folder because the sort sees <c>\</c> after letters and <c>/</c> before them. That has
/// no effect on handler order today (the two such pairs in the repository hold no handlers).</item>
/// <item>Which assemblies are scanned, and in what order, is load order. On a production server only
/// <c>SWLOR.Game.Server</c> holds handlers. When engine tests are enabled <c>SWLOR.Game.Server.EngineTests</c> is
/// loaded before the scan and is expected to be listed after the game assembly, so its handlers run after the game's for
/// the same script; <see cref="EngineTestHandlersRunAfterEveryGameHandlerOfTheSameScript"/> and its own snapshot record that.</item>
/// </list>
///
/// <para><b>What a snapshot change means.</b> A change here does not mean the code is wrong. It means a handler
/// moved, was added or removed. A change that no handler edit explains (a moved or new file, a namespace change) is
/// the unrelated drift described above; the reviewer must check that the new order still satisfies the real
/// dependencies (for example Redis before everything that reads it) before regenerating.</para>
///
/// <para>The tests scan <c>SWLOR.Game.Server</c> alone, not the test host's assemblies, through
/// <c>ScriptRegistry.LoadHandlersFromAssemblies</c>; <see cref="ScriptHandlerSeamTests"/> shows that the entry point
/// the server calls does the same discovery.</para>
/// </summary>
[TestFixture]
public class ScriptHandlerOrderTests
{
    private const string ProductionSnapshot = "SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.txt";
    private const string EngineTestSnapshot = "SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.EngineTests.txt";
    private const string RegenerateCommand =
        "SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Game.Server.Tests --no-build --filter \"FullyQualifiedName~ScriptHandlerOrderTests\"" +
        "   (PowerShell: $env:SWLOR_UPDATE_SNAPSHOTS='1'; dotnet test ...; Remove-Item Env:SWLOR_UPDATE_SNAPSHOTS)";

    private static Assembly GameAssembly => typeof(ScriptRegistry).Assembly;

    private static Assembly EngineTestAssembly => Assembly.Load(new AssemblyName("SWLOR.Game.Server.EngineTests"));

    [Test]
    public void ProductionHandlerOrderMatchesTheSnapshot()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly });

        var snapshot = ScriptHandlerOrderSnapshot.Render(
            "SWLOR script handler order: SWLOR.Game.Server only (a production server; engine tests disabled)",
            registry);

        SnapshotComparison.AssertMatches(ProductionSnapshot, snapshot, RegenerateCommand);
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
        var both = new ScriptRegistry();
        both.LoadHandlersFromAssemblies(new[] { GameAssembly, engineTests });

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
            "update ScriptHandlerOrderSnapshot.BootBuckets and regenerate the snapshot if that is intended");

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
