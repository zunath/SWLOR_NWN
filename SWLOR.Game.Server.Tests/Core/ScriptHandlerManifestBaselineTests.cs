using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Tests.Core.ScriptHandlerTestSupport;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The behaviour-neutral proof for the script handler manifest: with the manifest generated from the order reflection
/// gave when it was introduced, the registry runs every script's handlers in exactly the order the discovery code
/// before the manifest (<see cref="LegacyHandlerDiscovery"/>, kept from the P0-5 seam) produces.
///
/// <para><b>This fixture is meant to stop being true.</b> It holds only while reflection still returns handlers in the
/// order the manifest was generated from. The first change that legitimately reorders reflection (a handler class moved
/// to another namespace, an added file that shifts the compile order) makes it fail, and that change deletes this
/// fixture or rewrites its comparison. That is the point of the manifest: the order must not follow the layout.
/// The lasting guards are <see cref="ScriptHandlerManifestOrderTests"/> (the order follows the manifest under any
/// discovery order) and <see cref="ScriptHandlerOrderTests"/> (the manifest is complete).</para>
/// </summary>
[TestFixture]
public class ScriptHandlerManifestBaselineTests
{
    private const string WhenThisFails =
        "reflection no longer returns handlers in the order ScriptHandlerOrder.txt was generated from. That is expected after a " +
        "file or namespace move; the manifest keeps the old order, which is the behaviour to preserve. Delete this fixture in the " +
        "same change (see the class comment).";

    [Test]
    public void EveryScriptRunsItsGameHandlersInTheOrderTheDiscoveryBeforeTheManifestGave()
    {
        AssertSameOrderAsLegacyDiscovery(new[] { GameAssembly });
    }

    [Test]
    public void EveryScriptRunsItsHandlersInTheLegacyOrderWithTheEngineTestHandlersLoaded()
    {
        // The game assembly is scanned before the engine test assembly, as it is on a server with engine tests enabled.
        AssertSameOrderAsLegacyDiscovery(new[] { GameAssembly, EngineTestAssembly });
    }

    private static void AssertSameOrderAsLegacyDiscovery(System.Reflection.Assembly[] assemblies)
    {
        var legacy = LegacyHandlerDiscovery.Discover(assemblies);
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(assemblies);

        // Scripts appear in the same first-seen order for each kind.
        registry.GetActionScriptNames().Should().Equal(
            legacy.Where(h => !h.Conditional).Select(h => h.Script).Distinct(), WhenThisFails);
        registry.GetConditionalScriptNames().Should().Equal(
            legacy.Where(h => h.Conditional).Select(h => h.Script).Distinct(), WhenThisFails);

        // Every script runs the same handlers in the same order.
        var scripts = legacy.Select(h => h.Script).Distinct().ToArray();
        scripts.Should().NotBeEmpty();
        foreach (var script in scripts)
        {
            foreach (var conditional in new[] { false, true })
            {
                Names(registry, script, conditional).Should().Equal(
                    legacy.Where(h => h.Script == script && h.Conditional == conditional)
                        .Select(h => ScriptHandlerManifest.HandlerName(h.Method)),
                    $"'{script}' ({(conditional ? "conditional" : "action")}): {WhenThisFails}");
            }
        }
    }
}
