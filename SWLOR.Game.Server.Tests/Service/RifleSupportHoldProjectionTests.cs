using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using Stage = SWLOR.Game.Server.Service.RifleSupportHoldStage;

namespace SWLOR.Game.Server.Tests.Service;

public class RifleSupportHoldProjectionTests
{
    [Test]
    public void EveryObserverReceivesOneClearAndOneNativeRestore()
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "rf_old_i", 1000);
        state.PhysicalIdle(1).Should().Be("rf_old_i");
        state.Current(1, 10, 1000).Should().Be(Stage.Clear);
        state.Sent(1, 10, Stage.Clear, 1000);
        state.Current(1, 10, 1599).Should().Be(Stage.Hold);
        state.Current(1, 10, 1600).Should().Be(Stage.Restore);
        state.Sent(1, 10, Stage.Restore, 1600);
        state.Current(1, 10, 1601).Should().Be(Stage.None);
        state.Current(1, 11, 1601).Should().Be(Stage.Clear);
        state.Sent(1, 11, Stage.Clear, 1601);
        state.Current(1, 11, 1800).Should().Be(Stage.Hold);
        state.Current(1, 11, 1801).Should().Be(Stage.Restore);
    }

    [Test]
    public void AbilityOrUnequipCancelsUnsentClearsAndImmediatelyRestoresAffectedObservers()
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "xbowr", 0);
        state.Sent(1, 10, Stage.Clear, 0);
        state.Cancel(1);
        state.Current(1, 10, 1).Should().Be(Stage.Restore);
        state.Current(1, 11, 1).Should().Be(Stage.None);
    }

    [Test]
    public void ANewProfileSupersedesEveryPriorObserverStage()
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "rf_old_i", 0);
        state.Sent(1, 10, Stage.Clear, 0);
        state.Begin(1, "rf_new_i", 10);
        state.PhysicalIdle(1).Should().Be("rf_new_i");
        state.Current(1, 10, 10).Should().Be(Stage.Clear);
    }

    [TestCase(1)]
    [TestCase(3)]
    public void ReplacedRequestsRetainUnrestoredObserversWhenCancelled(int replacements)
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "rf_old_i", 0);
        state.Sent(1, 10, Stage.Clear, 0);
        state.Sent(1, 11, Stage.Clear, 0);
        state.Sent(1, 11, Stage.Restore, 600);
        for (var i = 0; i < replacements; i++) state.Begin(1, "rf_new_i", 700 + i);
        state.Cancel(1);
        state.Current(1, 10, 710).Should().Be(Stage.Restore);
        state.Current(1, 11, 710).Should().Be(Stage.None);
        state.Current(1, 12, 710).Should().Be(Stage.None);
        state.Sent(1, 10, Stage.Restore, 710);
        state.Current(1, 10, 711).Should().Be(Stage.None);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AnImmediateNativeShotIsNeverSuppressedByTheClearGrace(bool clearSent)
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "rf_old_i", 0);
        if (clearSent) state.Sent(1, 10, Stage.Clear, 0);
        state.ObserveAnimation(1, 9);
        state.Current(1, 10, 1).Should().Be(clearSent ? Stage.Restore : Stage.None);
        state.Current(1, 11, 1).Should().Be(Stage.None);
    }

    [Test]
    public void AnUnsentClearWaitsForTheShotToReturnInsteadOfBeingLost()
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "rf_old_i", 0);
        state.ObserveAnimation(1, 9);
        state.Current(1, 10, 1).Should().Be(Stage.None);
        state.ObserveAnimation(1, 0);
        state.Current(1, 10, 200).Should().Be(Stage.Clear);
    }

    [TestCase(0)]
    [TestCase(1)]
    public void RestingOrReadyAnimationsRetainTheClearGrace(int animation)
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "rf_old_i", 0);
        state.Sent(1, 10, Stage.Clear, 0);
        state.ObserveAnimation(1, animation);
        state.Current(1, 10, 1).Should().Be(Stage.Hold);
    }

    [Test]
    public void ExpiredRequestsDoNotClearNewObserversAndDestroyedObjectsAreForgotten()
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "xbowr", 0);
        state.Current(1, 10, 10000).Should().Be(Stage.None);
        state.Prune(_ => false, 1);
        state.Current(1, 10, 1).Should().Be(Stage.None);
        state.Begin(1, "rf_new_i", 10);
        state.Prune(_ => true, 30011);
        state.Current(1, 10, 30011).Should().Be(Stage.None);
    }

    [TestCase(0)]
    [TestCase(1)]
    public void AttackActionsDeferClearsAcrossTheGapsBetweenShots(int animation)
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "rf_old_i", 0);
        state.ObserveAnimation(1, animation, attackAction: true);
        state.Current(1, 10, 100).Should().Be(Stage.None);
        state.Current(1, 11, 100).Should().Be(Stage.None);
        state.ObserveAnimation(1, animation, attackAction: false);
        state.Current(1, 10, 250).Should().Be(Stage.Clear);
    }

    [Test]
    public void ResumingAnAttackActionRestoresAlreadyClearedObserversDuringNativeIdle()
    {
        var state = new RifleSupportHoldProjection();
        state.Begin(1, "rf_old_i", 0);
        state.Sent(1, 10, Stage.Clear, 0);
        state.ObserveAnimation(1, 1, attackAction: true);
        state.Current(1, 10, 100).Should().Be(Stage.Restore);
        state.Current(1, 11, 100).Should().Be(Stage.None);
        state.Sent(1, 10, Stage.Restore, 100);
        state.ObserveAnimation(1, 1, attackAction: false);
        state.Current(1, 10, 250).Should().Be(Stage.None);
        state.Current(1, 11, 250).Should().Be(Stage.Clear);
    }

}
