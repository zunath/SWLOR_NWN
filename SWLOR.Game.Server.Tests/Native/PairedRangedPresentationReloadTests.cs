using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Native;

/// <summary>
/// The muzzle anchors when a hot reload quiesces the old build (P4-15 closure audit): the pools are this build's memory, so every
/// actor's anchors, an NPC's as well as a player's, are destroyed rather than left in the world for good.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PairedRangedPresentationReloadTests
{
    private const uint Gunner = 701;
    private const uint Player = 702;

    private static readonly IDictionary Anchors = (IDictionary)typeof(PairedRangedPresentation)
        .GetField("Anchors", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

    [TearDown]
    public void ForgetAnchors() => Anchors.Clear();

    [Test]
    public void A_reload_destroys_the_anchors_of_every_actor_not_only_the_players_online()
    {
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", _ => true);
        using var scope = NWScriptRuntime.Override(engine);
        Anchors[Gunner] = (new uint[] { 801, 802 }, new long[2], 0L, 900u);
        Anchors[Player] = (new uint[] { 803 }, new long[1], 0L, 900u);

        PairedRangedPresentation.ReleaseAllForReload(new[] { Player });

        engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).Should().BeEquivalentTo(new uint[] { 801, 802, 803 });
        Anchors.Count.Should().Be(0);
    }
}
