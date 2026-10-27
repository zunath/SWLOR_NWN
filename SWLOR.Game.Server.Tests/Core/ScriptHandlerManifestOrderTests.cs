using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Tests.Core.ScriptHandlerTestSupport;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Proves what the script handler manifest is for: the order <see cref="ScriptRegistry"/> runs handlers in comes from the
/// manifest, so it no longer depends on the order reflection returns types and methods in (which follows namespaces and
/// file paths), and the boot path refuses to start without a readable manifest.
/// </summary>
[TestFixture]
public class ScriptHandlerManifestOrderTests
{
    private const string DbLoad = "SWLOR.Game.Server.Service.DB.Load";
    private const string OnModulePreload = "SWLOR.Game.Server.Feature.EventRegistration.OnModulePreload";
    private const string AbilityCacheData = "SWLOR.Game.Server.Service.Ability.CacheData";
    private const string AchievementLoad = "SWLOR.Game.Server.Service.Achievement.LoadAchievements";

    [Test]
    public void TheRegistryFollowsTheManifestWhateverOrderReflectionReturnsTheGameHandlersIn()
    {
        var manifest = ScriptHandlerManifest.LoadEmbedded(GameAssembly);

        foreach (var (label, methods) in GameDiscoveryOrders())
        {
            var registry = new ScriptRegistry();
            registry.LoadHandlers(methods, manifest);

            AssertRunsInManifestOrder(registry, manifest, label);
            registry.UnlistedHandlers.Should().BeEmpty(label);
            registry.StaleManifestEntries.Should().BeEmpty(label);

            // The one order the boot depends on: Redis is connected before the module preload handler reads it.
            var preload = Names(registry, ScriptName.OnModulePreload, false).ToList();
            preload.IndexOf(DbLoad).Should().BeGreaterThanOrEqualTo(0, label);
            preload.IndexOf(DbLoad).Should().BeLessThan(preload.IndexOf(OnModulePreload), label);
        }
    }

    [Test]
    public void TheDiscoveryOrdersTheLayoutTestUsesReallyDifferFromTheManifestOrder()
    {
        // Without the manifest the same discovery orders give different results, so the test above is not vacuous.
        var manifest = ScriptHandlerManifest.LoadEmbedded(GameAssembly);
        var differing = 0;

        foreach (var (label, methods) in GameDiscoveryOrders().Where(o => o.Label != "natural"))
        {
            var byDiscovery = new ScriptRegistry();
            byDiscovery.LoadHandlers(methods, ScriptHandlerManifest.Empty);

            var different = manifest.ScriptNames.Count(script =>
                !Names(byDiscovery, script, false).SequenceEqual(manifest.GetHandlers(script, false)) ||
                !Names(byDiscovery, script, true).SequenceEqual(manifest.GetHandlers(script, true)));
            different.Should().BeGreaterThan(0, $"the '{label}' discovery order must reorder at least one script");
            differing += different;
        }

        differing.Should().BeGreaterThan(0);
    }

    [Test]
    public void EngineTestHandlersRunAfterTheListedOnesInDiscoveryOrderWhateverTheLayout()
    {
        var manifest = ScriptHandlerManifest.LoadEmbedded(GameAssembly);
        var engineTests = EngineTestAssembly;
        var allHandlers = MethodsOf(GameAssembly, engineTests).Where(IsHandler).ToArray();

        foreach (var (label, methods) in DiscoveryOrders(allHandlers))
        {
            var registry = new ScriptRegistry();
            registry.LoadHandlers(methods, manifest);

            // What the engine test handlers alone would give in this discovery order.
            var engineOnly = new ScriptRegistry();
            engineOnly.LoadHandlers(methods.Where(m => m.DeclaringType!.Assembly == engineTests), ScriptHandlerManifest.Empty);

            foreach (var script in AllScripts(registry))
            {
                foreach (var conditional in new[] { false, true })
                {
                    var expected = manifest.GetHandlers(script, conditional).Concat(Names(engineOnly, script, conditional)).ToArray();
                    Names(registry, script, conditional).Should().Equal(expected,
                        $"'{script}' ({(conditional ? "conditional" : "action")}) in the '{label}' discovery order");
                }
            }

            registry.UnlistedHandlers.Should().NotBeEmpty(label);
            registry.StaleManifestEntries.Should().BeEmpty(label);
        }
    }

    [Test]
    public void AHandlerOrderInTheManifestOverridesTheOrderItWasDiscoveredIn()
    {
        // Swap the two handlers the boot depends on: the registry runs them swapped, so the manifest is the order's source.
        var swapped = ScriptHandlerManifest.Parse(
            SwapLines(ShippedManifestText(), "  action      " + DbLoad, "  action      " + OnModulePreload), "test");

        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly }, swapped);

        var preload = Names(registry, ScriptName.OnModulePreload, false).ToList();
        preload.IndexOf(OnModulePreload).Should().BeLessThan(preload.IndexOf(DbLoad));
        preload.Should().Equal(swapped.GetHandlers(ScriptName.OnModulePreload, false));
        registry.UnlistedHandlers.Should().BeEmpty();
    }

    [Test]
    public void UnlistedHandlersRunAfterTheListedOnesInDiscoveryOrderAndAreReported()
    {
        // Pretend two handlers of mod_cache_bef were never added to the manifest.
        var text = RemoveLines(ShippedManifestText(), "  action      " + AbilityCacheData, "  action      " + AchievementLoad);
        var manifest = ScriptHandlerManifest.Parse(text, "test");
        var listed = manifest.GetHandlers(ScriptName.OnModuleCacheBefore, false);
        var allHandlers = MethodsOf(GameAssembly).Where(IsHandler).ToArray();

        foreach (var (label, methods) in new[] { ("natural", allHandlers), ("reversed", Enumerable.Reverse(allHandlers).ToArray()) })
        {
            var registry = new ScriptRegistry();
            registry.LoadHandlers(methods, manifest);

            var discoveryOrder = new ScriptRegistry();
            discoveryOrder.LoadHandlers(methods, ScriptHandlerManifest.Empty);
            var unlistedInDiscoveryOrder = Names(discoveryOrder, ScriptName.OnModuleCacheBefore, false)
                .Where(name => name == AbilityCacheData || name == AchievementLoad).ToArray();
            unlistedInDiscoveryOrder.Should().HaveCount(2, label);

            Names(registry, ScriptName.OnModuleCacheBefore, false)
                .Should().Equal(listed.Concat(unlistedInDiscoveryOrder), label);
            registry.UnlistedHandlers.Should().BeEquivalentTo(new[]
            {
                $"{ScriptName.OnModuleCacheBefore} action {AbilityCacheData}",
                $"{ScriptName.OnModuleCacheBefore} action {AchievementLoad}",
            }, label);
            registry.StaleManifestEntries.Should().BeEmpty(label);
        }

        // The reversed discovery order really does put them in the other order.
        var natural = new ScriptRegistry();
        natural.LoadHandlers(allHandlers, manifest);
        var reversed = new ScriptRegistry();
        reversed.LoadHandlers(Enumerable.Reverse(allHandlers).ToArray(), manifest);
        Names(natural, ScriptName.OnModuleCacheBefore, false).TakeLast(2)
            .Should().Equal(Names(reversed, ScriptName.OnModuleCacheBefore, false).TakeLast(2).Reverse());
    }

    [Test]
    public void ListedHandlersThatNoLongerExistAreIgnoredAndReported()
    {
        const string goneHandler = "SWLOR.Game.Server.Service.Removed.Load";
        var text = ShippedManifestText().Replace("\r\n", "\n")
                .Replace("  action      " + OnModulePreload + "\n", "  action      " + OnModulePreload + "\n  action      " + goneHandler + "\n")
            + "\nzz_gone_script\n  action      SWLOR.Game.Server.Service.Removed.Other\n";
        var manifest = ScriptHandlerManifest.Parse(text, "test");

        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly }, manifest);

        registry.StaleManifestEntries.Should().BeEquivalentTo(new[]
        {
            $"{ScriptName.OnModulePreload} action {goneHandler}",
            "zz_gone_script action SWLOR.Game.Server.Service.Removed.Other",
        });
        registry.HasScript("zz_gone_script").Should().BeFalse("a script only the manifest knows about is not registered");
        registry.UnlistedHandlers.Should().BeEmpty();

        var preload = Names(registry, ScriptName.OnModulePreload, false);
        preload.Should().NotContain(goneHandler);
        preload.Should().Equal(ScriptHandlerManifest.LoadEmbedded(GameAssembly).GetHandlers(ScriptName.OnModulePreload, false));
    }

    [Test]
    public void TheBootPathFailsLoudlyWhenTheManifestCannotBeLoaded()
    {
        var failures = new (string Label, Func<ScriptHandlerManifest> Loader, Type Expected)[]
        {
            ("missing resource",
                () => ScriptHandlerManifest.LoadEmbedded(typeof(ScriptHandlerManifestOrderTests).Assembly),
                typeof(InvalidOperationException)),
            ("malformed text",
                () => ScriptHandlerManifest.Parse("mod_preload\n  action\n", "test"),
                typeof(InvalidDataException)),
            ("no handlers listed",
                () => ScriptHandlerManifest.Parse("# nothing here\n", "test"),
                typeof(InvalidDataException)),
            ("unreadable",
                () => throw new IOException("the resource stream failed"),
                typeof(IOException)),
            ("loader returned nothing",
                () => null!,
                typeof(InvalidOperationException)),
        };

        foreach (var (label, loader, expected) in failures)
        {
            var registry = new ScriptRegistry(loader);

            var viaServerEntryPoint = () => registry.LoadHandlersFromAssembly();
            var viaAssemblyList = () => registry.LoadHandlersFromAssemblies(new[] { GameAssembly });

            viaServerEntryPoint.Should().Throw<Exception>(label).Which.Should().BeOfType(expected, label);
            viaAssemblyList.Should().Throw<Exception>(label).Which.Should().BeOfType(expected, label);
            registry.HasScript(ScriptName.OnModulePreload).Should().BeFalse(
                $"{label}: nothing may be registered in discovery order when the manifest cannot be loaded");
        }
    }

    [Test]
    public void TheDefaultRegistryReadsTheManifestEmbeddedInTheGameAssembly()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly });

        AssertRunsInManifestOrder(registry, ScriptHandlerManifest.LoadEmbedded(GameAssembly), "default registry");
    }

    [Test]
    public void ARegistryCannotBeBuiltWithoutAManifestSource()
    {
        var create = () => new ScriptRegistry(null!);

        create.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// The game's handler methods in the order today's compiler gives them and in orders a file move or an added file
    /// could give them: types reversed, methods reversed inside their types, both, and seeded shuffles.
    /// </summary>
    private static IEnumerable<(string Label, MethodInfo[] Methods)> GameDiscoveryOrders()
    {
        var types = GameAssembly.GetTypes();

        yield return ("natural", types.SelectMany(t => t.GetMethods()).Where(IsHandler).ToArray());
        yield return ("types reversed",
            Enumerable.Reverse(types).SelectMany(t => t.GetMethods()).Where(IsHandler).ToArray());
        yield return ("methods reversed inside each type",
            types.SelectMany(t => Enumerable.Reverse(t.GetMethods())).Where(IsHandler).ToArray());
        yield return ("types and methods reversed",
            Enumerable.Reverse(types).SelectMany(t => Enumerable.Reverse(t.GetMethods())).Where(IsHandler).ToArray());

        foreach (var order in DiscoveryOrders(types.SelectMany(t => t.GetMethods()).Where(IsHandler).ToArray()).Skip(1))
            yield return order;
    }

    /// <summary>The given handlers as discovered, reversed, and in several seeded shuffles.</summary>
    private static IEnumerable<(string Label, MethodInfo[] Methods)> DiscoveryOrders(MethodInfo[] natural)
    {
        yield return ("natural", natural);
        yield return ("reversed", Enumerable.Reverse(natural).ToArray());
        for (var seed = 1; seed <= 5; seed++)
        {
            var shuffled = natural.ToArray();
            var random = new Random(seed);
            for (var i = shuffled.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            yield return ($"shuffled (seed {seed})", shuffled);
        }
    }

    private static IReadOnlyList<string> AllScripts(ScriptRegistry registry)
    {
        return registry.GetActionScriptNames().Concat(registry.GetConditionalScriptNames()).Distinct().ToArray();
    }

    private static void AssertRunsInManifestOrder(ScriptRegistry registry, ScriptHandlerManifest manifest, string label)
    {
        AllScripts(registry).Should().BeEquivalentTo(manifest.ScriptNames, label);

        foreach (var script in manifest.ScriptNames)
        {
            Names(registry, script, false).Should().Equal(manifest.GetHandlers(script, false), $"{label}: action handlers of '{script}'");
            Names(registry, script, true).Should().Equal(manifest.GetHandlers(script, true), $"{label}: conditional handlers of '{script}'");
        }
    }

    private static string RemoveLines(string text, params string[] lines)
    {
        var result = text.Replace("\r\n", "\n");
        foreach (var line in lines)
        {
            var before = result;
            result = result.Replace(line + "\n", string.Empty);
            result.Should().NotBe(before, $"'{line.Trim()}' must be a line of the shipped manifest");
        }

        return result;
    }

    private static string SwapLines(string text, string first, string second)
    {
        const string marker = "<swap-marker>";
        var result = text.Replace("\r\n", "\n");
        result.Should().Contain(first + "\n").And.Contain(second + "\n");
        return result.Replace(first + "\n", marker + "\n").Replace(second + "\n", first + "\n").Replace(marker + "\n", second + "\n");
    }
}
