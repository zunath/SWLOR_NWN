using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class KatarAnimationRemapPlanTests
{
    private static readonly string[] NoActiveSwing = Array.Empty<string>();

    private static Dictionary<string, string> FullRemap() =>
        KatarAnimationRemapPlan.Pairs.ToDictionary(pair => pair.Source, pair => pair.Replacement);

    [Test]
    public void SavedRemapWithoutKatar_IsRemoved()
    {
        // A character saved while it held a katar loads with every mapping and no session state.
        KatarAnimationRemapPlan.GetChanges(false, false, FullRemap(), NoActiveSwing)
            .Should().BeEquivalentTo(KatarAnimationRemapPlan.Pairs.Select(pair => (pair.Source, string.Empty)));
    }

    [Test]
    public void PartialSavedRemapWithoutKatar_RemovesWhatRemains()
    {
        var current = new Dictionary<string, string> { ["1hreadyr"] = "nwreadyr", ["1hparryl"] = "dodges" };

        KatarAnimationRemapPlan.GetChanges(false, false, current, NoActiveSwing)
            .Should().BeEquivalentTo(new[] { ("1hreadyr", string.Empty), ("1hparryl", string.Empty) });
    }

    [Test]
    public void OtherOwnersMappingsWithoutKatar_AreKept()
    {
        var current = new Dictionary<string, string>
        {
            ["1hslashl"] = "sw_doub_stri",
            ["custom1lp"] = "sw_snaprush",
            ["2hreadyr"] = "nwreadyr"
        };

        KatarAnimationRemapPlan.GetChanges(false, false, current, NoActiveSwing).Should().BeEmpty();
    }

    [Test]
    public void NoMappingsWithoutKatar_ChangesNothing()
    {
        KatarAnimationRemapPlan.GetChanges(false, false, new Dictionary<string, string>(), NoActiveSwing).Should().BeEmpty();
    }

    [Test]
    public void KatarWithoutMappings_AppliesEveryPair()
    {
        KatarAnimationRemapPlan.GetChanges(true, false, new Dictionary<string, string>(), NoActiveSwing)
            .Should().BeEquivalentTo(KatarAnimationRemapPlan.Pairs);
    }

    [Test]
    public void KatarWithSavedRemap_ChangesNothing()
    {
        KatarAnimationRemapPlan.GetChanges(true, false, FullRemap(), NoActiveSwing).Should().BeEmpty();
    }

    [Test]
    public void KatarAfterQueuedSwingReleased_RefillsOnlyTheClearedKeys()
    {
        var current = FullRemap();
        current.Remove("1hslashl");
        current.Remove("1hstab");

        KatarAnimationRemapPlan.GetChanges(true, false, current, NoActiveSwing)
            .Should().BeEquivalentTo(new[] { ("1hslashl", "nwslashl"), ("1hstab", "nwstab") });
    }

    [Test]
    public void KatarWithQueuedSwing_KeepsTheAbilityClip()
    {
        var current = FullRemap();
        current["1hslashl"] = "sw_doub_stri";

        KatarAnimationRemapPlan.GetChanges(true, false, current, QueuedAttackAnimationPlayback.SwingKeys).Should().BeEmpty();
    }

    [Test]
    public void KatarWithSavedQueuedClipAndNoPlayback_ReplacesTheClip()
    {
        // A character saved mid queued swing loads with the ability clip but no playback to release it.
        var current = FullRemap();
        current["1hslashl"] = "sw_doub_stri";
        current["1hstab"] = "sw_doub_stri";

        KatarAnimationRemapPlan.GetChanges(true, false, current, NoActiveSwing)
            .Should().BeEquivalentTo(new[] { ("1hslashl", "nwslashl"), ("1hstab", "nwstab") });
    }

    [Test]
    public void KatarWithQueuedSwing_StillFixesTheReadyPose()
    {
        var current = new Dictionary<string, string> { ["1hslashl"] = "sw_doub_stri" };

        var changes = KatarAnimationRemapPlan.GetChanges(true, false, current, QueuedAttackAnimationPlayback.SwingKeys);

        changes.Should().NotContain(change => change.Source == "1hslashl");
        changes.Should().Contain(("1hreadyr", "nwreadyr"));
    }

    [Test]
    public void PowerGloves_DropTheRightArmSwordCarryLayers()
    {
        KatarAnimationRemapPlan.GetChanges(true, true, new Dictionary<string, string>(), NoActiveSwing)
            .Should().BeEquivalentTo(KatarAnimationRemapPlan.Pairs.Concat(new[]
            {
                ("walk_swordr", NamedAnimationPlayback.NoHoldName),
                ("run_swordr", NamedAnimationPlayback.NoHoldName),
            }));
    }

    [Test]
    public void PowerGlovesWithQueuedSwing_StillDropTheCarryLayers()
    {
        var current = FullRemap();
        current["1hslashl"] = "sw_doub_stri";

        KatarAnimationRemapPlan.GetChanges(true, true, current, QueuedAttackAnimationPlayback.SwingKeys)
            .Should().BeEquivalentTo(KatarAnimationRemapPlan.PowerGloveCarryPairs);
    }

    [Test]
    public void KatarAfterPowerGloves_RestoresOnlyTheCarryLayers()
    {
        var current = FullRemap();
        foreach (var (source, replacement) in KatarAnimationRemapPlan.PowerGloveCarryPairs)
            current[source] = replacement;

        KatarAnimationRemapPlan.GetChanges(true, false, current, NoActiveSwing)
            .Should().BeEquivalentTo(new[] { ("walk_swordr", string.Empty), ("run_swordr", string.Empty) });
    }

    [Test]
    public void SavedPowerGloveMappingsWithoutGloves_AreAllRemoved()
    {
        var current = FullRemap();
        foreach (var (source, replacement) in KatarAnimationRemapPlan.PowerGloveCarryPairs)
            current[source] = replacement;

        var changes = KatarAnimationRemapPlan.GetChanges(false, false, current, NoActiveSwing);

        changes.Should().HaveCount(current.Count).And.OnlyContain(change => change.Replacement == string.Empty);
    }

    [Test]
    public void RepositioningDash_KeepsTheCarryLayerItHolds()
    {
        // Twin Pistol repositioning maps run_swordr to the same missing clip for the length of its dash.
        var current = new Dictionary<string, string> { ["run_swordr"] = NamedAnimationPlayback.NoHoldName };

        KatarAnimationRemapPlan.GetChanges(false, false, current, NoActiveSwing, carryLayersInUse: true).Should().BeEmpty();
        KatarAnimationRemapPlan.GetChanges(false, false, current, NoActiveSwing)
            .Should().BeEquivalentTo(new[] { ("run_swordr", string.Empty) });
    }
}
