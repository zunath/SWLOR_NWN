using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PvpCombatService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The PvP combat window: any PvP damage holds it open for 60 seconds, and while it is open a request to turn the PvP flag
/// off or change identity waits and takes effect when it ends.
/// </summary>
public class PvpCombatWindowRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    // The window

    [Test]
    public void TheWindow_IsSixtySeconds()
    {
        PvpCombatWindowRules.WindowSeconds.Should().Be(60.0);
        PvpCombatWindowRules.Extend(null, Now).Should().Be(Now.AddSeconds(60));
    }

    [Test]
    public void AnotherHit_PushesTheWindowOut_AndNeverBringsItIn()
    {
        var first = PvpCombatWindowRules.Extend(null, Now);

        PvpCombatWindowRules.Extend(first, Now.AddSeconds(30)).Should().Be(Now.AddSeconds(90));
        PvpCombatWindowRules.Extend(Now.AddSeconds(500), Now).Should().Be(Now.AddSeconds(500));
    }

    [Test]
    public void TheWindow_IsOpenUntilItsEnd()
    {
        var until = Now.AddSeconds(60);

        PvpCombatWindowRules.IsOpen(until, Now.AddSeconds(59)).Should().BeTrue();
        PvpCombatWindowRules.IsOpen(until, until).Should().BeFalse();
        PvpCombatWindowRules.IsOpen(null, Now).Should().BeFalse();
        PvpCombatWindowRules.GetSecondsLeft(until, Now.AddSeconds(50)).Should().Be(10);
        PvpCombatWindowRules.GetSecondsLeft(until, until).Should().Be(0);
        PvpCombatWindowRules.GetSecondsLeft(null, Now).Should().Be(0);
    }

    // The PvP flag

    [Test]
    public void TurningTheFlagOff_WithNoWindow_GoesAheadAtOnce()
    {
        var requests = new PvpCombatRequests();

        PvpCombatWindowRules.RequestFlag(windowOpen: false, flagIsOn: true, wantOn: false, requests)
            .Should().Be(PvpCombatChangeOutcome.Applied);
        requests.HasAny.Should().BeFalse();
    }

    [Test]
    public void TurningTheFlagOff_DuringTheWindow_IsQueued_AndTheFlagStaysOn()
    {
        var requests = new PvpCombatRequests();

        PvpCombatWindowRules.RequestFlag(windowOpen: true, flagIsOn: true, wantOn: false, requests)
            .Should().Be(PvpCombatChangeOutcome.Queued);
        requests.IsFlagOffQueued.Should().BeTrue();
    }

    [Test]
    public void AskingTwiceToTurnTheFlagOff_QueuesItOnce()
    {
        var requests = new PvpCombatRequests();
        PvpCombatWindowRules.RequestFlag(true, true, false, requests);

        PvpCombatWindowRules.RequestFlag(true, true, false, requests).Should().Be(PvpCombatChangeOutcome.Unchanged);
        requests.IsFlagOffQueued.Should().BeTrue();
    }

    [Test]
    public void TurningTheFlagBackOn_UndoesTheQueuedTurnOff()
    {
        var requests = new PvpCombatRequests();
        PvpCombatWindowRules.RequestFlag(true, true, false, requests);

        PvpCombatWindowRules.RequestFlag(true, true, true, requests).Should().Be(PvpCombatChangeOutcome.Cancelled);
        requests.IsFlagOffQueued.Should().BeFalse();
    }

    [Test]
    public void TurningTheFlagOn_IsNeverHeldBack()
    {
        var requests = new PvpCombatRequests();

        PvpCombatWindowRules.RequestFlag(windowOpen: true, flagIsOn: false, wantOn: true, requests)
            .Should().Be(PvpCombatChangeOutcome.Applied);
        requests.HasAny.Should().BeFalse();
    }

    [Test]
    public void AFlagAlreadyOff_NeedsNothing()
    {
        PvpCombatWindowRules.RequestFlag(true, false, false, new PvpCombatRequests()).Should().Be(PvpCombatChangeOutcome.Unchanged);
        PvpCombatWindowRules.RequestFlag(false, true, true, new PvpCombatRequests()).Should().Be(PvpCombatChangeOutcome.Unchanged);
    }

    // The identity shown

    [Test]
    public void ChangingIdentity_WithNoWindow_GoesAheadAtOnce()
    {
        var requests = new PvpCombatRequests();

        PvpCombatWindowRules.RequestIdentity(false, string.Empty, "disguise-1", requests).Should().Be(PvpCombatChangeOutcome.Applied);
        PvpCombatWindowRules.RequestIdentity(false, "disguise-1", string.Empty, requests).Should().Be(PvpCombatChangeOutcome.Applied);
        requests.HasAny.Should().BeFalse();
    }

    [Test]
    public void ChangingIdentity_DuringTheWindow_IsQueued_ForBothPuttingOnAndTakingOffADisguise()
    {
        var putOn = new PvpCombatRequests();
        PvpCombatWindowRules.RequestIdentity(true, string.Empty, "disguise-1", putOn).Should().Be(PvpCombatChangeOutcome.Queued);
        putOn.IsIdentityChangeQueued.Should().BeTrue();
        putOn.QueuedDisguiseId.Should().Be("disguise-1");

        var takeOff = new PvpCombatRequests();
        PvpCombatWindowRules.RequestIdentity(true, "disguise-1", string.Empty, takeOff).Should().Be(PvpCombatChangeOutcome.Queued);
        takeOff.IsIdentityChangeQueued.Should().BeTrue();
        takeOff.QueuedDisguiseId.Should().BeEmpty("an empty id means the player's own character");
    }

    [Test]
    public void ANewIdentityRequest_ReplacesTheQueuedOne()
    {
        var requests = new PvpCombatRequests();
        PvpCombatWindowRules.RequestIdentity(true, string.Empty, "disguise-1", requests);

        PvpCombatWindowRules.RequestIdentity(true, string.Empty, "disguise-2", requests).Should().Be(PvpCombatChangeOutcome.Queued);
        requests.QueuedDisguiseId.Should().Be("disguise-2");
    }

    [Test]
    public void AskingForTheIdentityAlreadyShown_UndoesTheQueuedChange()
    {
        var requests = new PvpCombatRequests();
        PvpCombatWindowRules.RequestIdentity(true, string.Empty, "disguise-1", requests);

        PvpCombatWindowRules.RequestIdentity(true, string.Empty, string.Empty, requests).Should().Be(PvpCombatChangeOutcome.Cancelled);
        requests.IsIdentityChangeQueued.Should().BeFalse();
        requests.QueuedDisguiseId.Should().BeEmpty();
    }

    [Test]
    public void AskingForTheIdentityAlreadyShown_WithNothingQueued_ChangesNothing()
    {
        PvpCombatWindowRules.RequestIdentity(true, "disguise-1", "disguise-1", new PvpCombatRequests())
            .Should().Be(PvpCombatChangeOutcome.Unchanged);
    }

    // When the window ends

    [Test]
    public void QueuedRequests_TakeEffectOnlyOnceTheWindowIsClosed()
    {
        var requests = new PvpCombatRequests();
        PvpCombatWindowRules.RequestFlag(true, true, false, requests);
        PvpCombatWindowRules.RequestIdentity(true, string.Empty, "disguise-1", requests);

        PvpCombatWindowRules.ShouldApplyQueued(requests, windowOpen: true).Should().BeFalse();
        PvpCombatWindowRules.ShouldApplyQueued(requests, windowOpen: false).Should().BeTrue();
    }

    [Test]
    public void NothingQueued_NeverApplies()
    {
        PvpCombatWindowRules.ShouldApplyQueued(new PvpCombatRequests(), windowOpen: false).Should().BeFalse();
    }

    [Test]
    public void ClearingTheRequests_LeavesNothingWaiting()
    {
        var requests = new PvpCombatRequests();
        PvpCombatWindowRules.RequestFlag(true, true, false, requests);
        PvpCombatWindowRules.RequestIdentity(true, string.Empty, "disguise-1", requests);

        requests.Clear();

        requests.HasAny.Should().BeFalse();
        requests.QueuedDisguiseId.Should().BeEmpty();
    }

    [Test]
    public void TheQueue_SurvivesBeingSavedWithThePlayer()
    {
        var requests = new PvpCombatRequests();
        PvpCombatWindowRules.RequestFlag(true, true, false, requests);
        PvpCombatWindowRules.RequestIdentity(true, string.Empty, "disguise-1", requests);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(requests);
        var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<PvpCombatRequests>(json);

        loaded.IsFlagOffQueued.Should().BeTrue();
        loaded.IsIdentityChangeQueued.Should().BeTrue();
        loaded.QueuedDisguiseId.Should().Be("disguise-1");
    }
}
