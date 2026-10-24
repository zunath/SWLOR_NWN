using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;

namespace SWLOR.Game.Server.Tests.Feature;

public class KatarAnimationRemapPlanTests
{
    private static Dictionary<string, string> FullRemap() =>
        KatarAnimationRemapPlan.Pairs.ToDictionary(pair => pair.Source, pair => pair.Replacement);

    [Test]
    public void SavedRemapWithoutKatar_IsRemoved()
    {
        // A character saved while it held a katar loads with every mapping and no session state.
        KatarAnimationRemapPlan.GetChanges(false, FullRemap())
            .Should().BeEquivalentTo(KatarAnimationRemapPlan.Pairs.Select(pair => (pair.Source, string.Empty)));
    }

    [Test]
    public void PartialSavedRemapWithoutKatar_RemovesWhatRemains()
    {
        var current = new Dictionary<string, string> { ["1hreadyr"] = "nwreadyr", ["1hparryl"] = "dodges" };

        KatarAnimationRemapPlan.GetChanges(false, current)
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

        KatarAnimationRemapPlan.GetChanges(false, current).Should().BeEmpty();
    }

    [Test]
    public void NoMappingsWithoutKatar_ChangesNothing()
    {
        KatarAnimationRemapPlan.GetChanges(false, new Dictionary<string, string>()).Should().BeEmpty();
    }

    [Test]
    public void KatarWithoutMappings_AppliesEveryPair()
    {
        KatarAnimationRemapPlan.GetChanges(true, new Dictionary<string, string>())
            .Should().BeEquivalentTo(KatarAnimationRemapPlan.Pairs);
    }

    [Test]
    public void KatarWithSavedRemap_ChangesNothing()
    {
        KatarAnimationRemapPlan.GetChanges(true, FullRemap()).Should().BeEmpty();
    }

    [Test]
    public void KatarAfterQueuedSwingReleased_RefillsOnlyTheClearedKeys()
    {
        var current = FullRemap();
        current.Remove("1hslashl");
        current.Remove("1hstab");

        KatarAnimationRemapPlan.GetChanges(true, current)
            .Should().BeEquivalentTo(new[] { ("1hslashl", "nwslashl"), ("1hstab", "nwstab") });
    }

    [Test]
    public void KatarWithQueuedSwing_KeepsTheAbilityClip()
    {
        var current = FullRemap();
        current["1hslashl"] = "sw_doub_stri";

        KatarAnimationRemapPlan.GetChanges(true, current).Should().BeEmpty();
    }
}
