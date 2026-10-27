using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Proves the seams added to <see cref="ScriptRegistry"/> for <see cref="ScriptHandlerOrderTests"/> change nothing the
/// server does: <c>LoadHandlersFromAssembly</c> (what <c>ServerBootstrapper</c> calls) still scans every assembly loaded
/// in the AppDomain, and the registry holds exactly the handlers the discovery code it replaced produced. The order
/// they run in is the manifest's (<see cref="ScriptHandlerManifestOrderTests"/>).
/// </summary>
[TestFixture]
public class ScriptHandlerSeamTests
{
    private static Assembly GameAssembly => typeof(ScriptRegistry).Assembly;

    [Test]
    public void LoadHandlersFromAssemblyScansEveryAssemblyLoadedInTheAppDomain()
    {
        // Make sure the second assembly with handlers is loaded, so the scan has something besides the game assembly.
        var engineTests = Assembly.Load(new AssemblyName("SWLOR.Game.Server.EngineTests"));
        engineTests.Should().NotBeNull();

        // Scanning loads the assemblies the scanned types depend on, which grows the AppDomain. Scan until it stops
        // growing so the two registries below see the same assemblies.
        for (var pass = 0; pass < 10; pass++)
        {
            var before = AppDomain.CurrentDomain.GetAssemblies().Length;
            new ScriptRegistry().LoadHandlersFromAssembly();
            if (AppDomain.CurrentDomain.GetAssemblies().Length == before)
                break;
        }

        var loaded = AppDomain.CurrentDomain.GetAssemblies();
        var viaServerEntryPoint = new ScriptRegistry();
        viaServerEntryPoint.LoadHandlersFromAssembly();
        var viaExplicitList = new ScriptRegistry();
        viaExplicitList.LoadHandlersFromAssemblies(loaded);

        AppDomain.CurrentDomain.GetAssemblies().Should().HaveCount(loaded.Length,
            "the scans must not have loaded another assembly, or the two registries saw different sets");
        Describe(viaServerEntryPoint).Should().NotBeEmpty();
        Describe(viaServerEntryPoint).Should().Equal(Describe(viaExplicitList));
    }

    [Test]
    public void RegistryHoldsExactlyWhatTheReplacedDiscoveryProduced()
    {
        var engineTests = Assembly.Load(new AssemblyName("SWLOR.Game.Server.EngineTests"));
        var sets = new[]
        {
            new[] { GameAssembly },
            new[] { GameAssembly, engineTests },
            AppDomain.CurrentDomain.GetAssemblies(),
        };

        foreach (var assemblies in sets)
        {
            var legacy = LegacyHandlerDiscovery.Discover(assemblies);
            var registry = new ScriptRegistry();
            registry.LoadHandlersFromAssemblies(assemblies);

            // Same scripts for each kind and the same handlers inside each script. Only the order is the manifest's
            // to decide now, and ScriptHandlerManifestBaselineTests compares that with this discovery.
            registry.GetActionScriptNames().Should().BeEquivalentTo(
                legacy.Where(h => !h.Conditional).Select(h => h.Script).Distinct());
            registry.GetConditionalScriptNames().Should().BeEquivalentTo(
                legacy.Where(h => h.Conditional).Select(h => h.Script).Distinct());

            foreach (var script in legacy.Select(h => h.Script).Distinct())
            {
                registry.GetActionScripts(script).Select(h => ScriptHandlerOrderSnapshot.FullName(h.Action.Method))
                    .Should().BeEquivalentTo(legacy.Where(h => h.Script == script && !h.Conditional)
                        .Select(h => ScriptHandlerOrderSnapshot.FullName(h.Method)));
                registry.GetConditionalScripts(script).Select(h => ScriptHandlerOrderSnapshot.FullName(h.Action.Method))
                    .Should().BeEquivalentTo(legacy.Where(h => h.Script == script && h.Conditional)
                        .Select(h => ScriptHandlerOrderSnapshot.FullName(h.Method)));
            }

            registry.HasScript("not_a_script").Should().BeFalse();
        }
    }

    [Test]
    public void LoadingAgainReplacesWhatWasRegistered()
    {
        var engineTests = Assembly.Load(new AssemblyName("SWLOR.Game.Server.EngineTests"));
        var reused = new ScriptRegistry();
        reused.LoadHandlersFromAssemblies(new[] { GameAssembly, engineTests });
        reused.LoadHandlersFromAssemblies(new[] { GameAssembly });

        var fresh = new ScriptRegistry();
        fresh.LoadHandlersFromAssemblies(new[] { GameAssembly });

        Describe(reused).Should().Equal(Describe(fresh));
    }

    private static List<string> Describe(ScriptRegistry registry)
    {
        var lines = new List<string>();
        foreach (var script in registry.GetActionScriptNames())
        {
            lines.Add("action " + script);
            lines.AddRange(registry.GetActionScripts(script).Select(h => ScriptHandlerOrderSnapshot.FullName(h.Action.Method)));
        }

        foreach (var script in registry.GetConditionalScriptNames())
        {
            lines.Add("conditional " + script);
            lines.AddRange(registry.GetConditionalScripts(script).Select(h => ScriptHandlerOrderSnapshot.FullName(h.Action.Method)));
        }

        return lines;
    }
}
