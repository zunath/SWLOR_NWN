using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Tests.Service;

public class RifleSupportAnimationPlaybackTests
{
    private static RifleSupportPoses Poses(string name) => new(name + "i", name + "r", name + "s");

    [Test]
    public void FirstProjectionClearsThePhysicalHoldOnceAndRepeatedRefreshesKeepTheMappings()
    {
        var runtime = new FakeRifleSupportAnimationRuntime();
        var playback = new RifleSupportAnimationPlayback(runtime);
        var poses = Poses("rf_a_");
        playback.Apply(1, poses);
        runtime.Maps["xbowr"].Should().Be(poses.Idle);
        runtime.Maps["xbowrdy"].Should().Be(poses.Ready);
        runtime.Maps["xbowshot"].Should().Be(poses.Shot);
        runtime.Maps["pause1"].Should().Be(poses.BaseIdle);
        runtime.Maps["plpause1"].Should().Be("xbowr");
        runtime.Maps["dodges"].Should().Be(poses.Idle);
        playback.Apply(1, poses);
        runtime.ClearCalls.Should().Be(1);
        runtime.Callbacks[0]();
        runtime.Maps["plpause1"].Should().BeEmpty();
        runtime.Maps["dodges"].Should().BeEmpty();
        playback.Refresh(1);
        runtime.ClearCalls.Should().Be(1);
    }

    [Test]
    public void RapidAppearanceChangesApplyOnlyTheLatestSelectionAndIgnoreOldCallbacks()
    {
        var runtime = new FakeRifleSupportAnimationRuntime();
        var playback = new RifleSupportAnimationPlayback(runtime);
        playback.Apply(1, Poses("rf_a_"));
        playback.Apply(1, Poses("rf_b_"));
        playback.Apply(1, Poses("rf_c_"));
        runtime.Callbacks[0]();
        runtime.Maps["xbowr"].Should().Be("rf_c_i");
        runtime.Maps["pause1"].Should().Be("rf_c_b");
        runtime.Maps["plpause1"].Should().Be("rf_a_i");
        runtime.ClearCalls.Should().Be(2);
        runtime.Callbacks[0]();
        runtime.Maps["dodges"].Should().Be("rf_c_i");
        runtime.Callbacks[1]();
        runtime.Maps["dodges"].Should().BeEmpty();
        runtime.Maps["xbowr"].Should().Be("rf_c_i");
        runtime.Maps["pause1"].Should().Be("rf_c_b");
    }

    [Test]
    public void UnequipDuringTheClearRemovesOwnedMappingsWithoutStartingAnotherPose()
    {
        var runtime = new FakeRifleSupportAnimationRuntime();
        var playback = new RifleSupportAnimationPlayback(runtime);
        playback.Apply(1, Poses("rf_a_"));
        playback.Apply(1, null);
        runtime.Callbacks[0]();
        runtime.Maps.Values.Should().OnlyContain(value => value == "");
        runtime.ClearCalls.Should().Be(1);
    }

    [Test]
    public void TemporaryAbilitiesKeepTheirMappingsAndEquipmentRestoresAfterTheirRelease()
    {
        var runtime = new FakeRifleSupportAnimationRuntime();
        var playback = new RifleSupportAnimationPlayback(runtime);
        playback.Apply(1, Poses("rf_a_"));
        runtime.Callbacks[0]();
        runtime.Temporary = true;
        runtime.Maps["xbowr"] = NamedAnimationPlayback.NoHoldName;
        runtime.Maps["dodges"] = "sw_ability";
        playback.Apply(1, Poses("rf_b_"));
        runtime.Maps["xbowr"].Should().Be(NamedAnimationPlayback.NoHoldName);
        runtime.Maps["dodges"].Should().Be("sw_ability");
        runtime.Temporary = false;
        runtime.Maps["xbowr"] = runtime.Maps["dodges"] = "";
        playback.Refresh(1);
        runtime.Maps["xbowr"].Should().Be("rf_b_i");
        runtime.ClearCalls.Should().Be(2);
    }

    [Test]
    public void AnAbilityStartingDuringTheClearIsNotOverwrittenByItsCallback()
    {
        var runtime = new FakeRifleSupportAnimationRuntime();
        var playback = new RifleSupportAnimationPlayback(runtime);
        playback.Apply(1, Poses("rf_a_"));
        runtime.Temporary = true;
        runtime.Maps["xbowr"] = NamedAnimationPlayback.NoHoldName;
        runtime.Maps["dodges"] = "sw_ability";
        runtime.Maps["plpause1"] = "xbowr";
        runtime.Callbacks[0]();
        runtime.Maps["dodges"].Should().Be("sw_ability");
        runtime.Maps["plpause1"].Should().Be("xbowr");
        runtime.Temporary = false;
        runtime.Maps["xbowr"] = runtime.Maps["dodges"] = runtime.Maps["plpause1"] = "";
        playback.Refresh(1);
        runtime.Maps["xbowr"].Should().Be("rf_a_i");
        runtime.ClearCalls.Should().Be(1);
    }

    [Test]
    public void ForeignRifleAndCarrierOwnersKeepTheirKeysUntilTheyReleaseThem()
    {
        var runtime = new FakeRifleSupportAnimationRuntime();
        var playback = new RifleSupportAnimationPlayback(runtime);
        runtime.Maps["xbowr"] = "sw_foreign";
        playback.Apply(1, Poses("rf_a_"));
        runtime.Maps["xbowr"].Should().Be("sw_foreign");
        runtime.ClearCalls.Should().Be(0);
        runtime.Maps["xbowr"] = "";
        runtime.Maps["dodges"] = "sw_foreign";
        playback.Refresh(1);
        runtime.Maps["xbowr"].Should().Be("rf_a_i");
        runtime.Maps["dodges"].Should().Be("sw_foreign");
        runtime.ClearCalls.Should().Be(0);
        runtime.Maps["dodges"] = "";
        playback.Refresh(1);
        runtime.ClearCalls.Should().Be(1);
    }

    [Test]
    public void ForeignBaseIdleDelaysFittedMappingsAndUnequipLeavesItsOwnerInControl()
    {
        var runtime = new FakeRifleSupportAnimationRuntime();
        var playback = new RifleSupportAnimationPlayback(runtime);
        runtime.Maps["pause1"] = "sw_foreign";
        playback.Apply(1, Poses("rf_a_"));
        runtime.Maps.Should().HaveCount(1);
        runtime.ClearCalls.Should().Be(0);
        runtime.Maps["pause1"] = "";
        playback.Refresh(1);
        runtime.Callbacks[0]();
        runtime.Maps["pause1"].Should().Be("rf_a_b");
        runtime.Maps["pause1"] = "sw_foreign";
        playback.Apply(1, null);
        runtime.Maps["pause1"].Should().Be("sw_foreign");
        runtime.Maps["pause1"] = "";
        playback.Refresh(1);
        runtime.Maps.Values.Should().OnlyContain(value => value == "");
    }

    [Test]
    public void SchedulingFailureReleasesBorrowedCarriersAndCanBeRetried()
    {
        var runtime = new FakeRifleSupportAnimationRuntime { FailSchedule = true };
        var playback = new RifleSupportAnimationPlayback(runtime);
        Action apply = () => playback.Apply(1, Poses("rf_a_"));
        apply.Should().Throw<InvalidOperationException>();
        runtime.Maps["dodges"].Should().BeEmpty();
        runtime.Maps["plpause1"].Should().BeEmpty();
        runtime.FailSchedule = false;
        playback.Refresh(1);
        runtime.ClearCalls.Should().Be(2);
    }

    [Test]
    public void DestroyedActorCallbacksCannotChangeAReusedObjectId()
    {
        var runtime = new FakeRifleSupportAnimationRuntime();
        var playback = new RifleSupportAnimationPlayback(runtime);
        playback.Apply(1, Poses("rf_a_"));
        runtime.Valid = false;
        playback.Prune();
        runtime.Valid = true;
        runtime.Maps.Clear();
        playback.Apply(1, Poses("rf_b_"));
        runtime.Callbacks[0]();
        runtime.Maps["xbowr"].Should().Be("rf_b_i");
        runtime.Maps["dodges"].Should().Be("rf_b_i");
    }
}
