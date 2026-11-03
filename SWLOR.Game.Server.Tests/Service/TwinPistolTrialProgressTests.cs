using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Service;

public class TwinPistolTrialProgressTests
{
    private static readonly DateTime Start = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void SustainedTrial_RequiresSixLandedShotsFromEachGun_OnOneEnemy()
    {
        var trial = new TwinPistolTrialProgress();
        for (var i = 0; i < 6; i++)
            trial.RecordOrdinaryHit(Start.AddSeconds(i), 10, false).Should().BeFalse();
        for (var i = 0; i < 5; i++)
            trial.RecordOrdinaryHit(Start.AddSeconds(i + 6), 10, true).Should().BeFalse();
        trial.RecordOrdinaryHit(Start.AddSeconds(11), 10, true).Should().BeTrue();
    }

    [Test]
    public void SustainedTrial_TargetChangeOrTimeoutResetsTheAttempt()
    {
        var trial = new TwinPistolTrialProgress();
        for (var i = 0; i < 11; i++)
            trial.RecordOrdinaryHit(Start.AddSeconds(i), 10, i % 2 == 0).Should().BeFalse();
        trial.RecordTargetChange(20);
        trial.RecordTargetChange(10);
        trial.RecordOrdinaryHit(Start.AddSeconds(11), 10, false).Should().BeFalse();
        trial.RecordOrdinaryHit(Start.AddSeconds(42), 10, true).Should().BeFalse();
    }

    [Test]
    public void SequenceTrial_TempoThenLeadThenAClosingShotSpendingTwoCadence()
    {
        var trial = new TwinPistolTrialProgress();
        trial.RecordAbility(Start, true, PerkType.TempoShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(2), true, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(4), true, PerkType.ClosingShot, 2).Should().BeTrue();
    }

    [Test]
    public void SequenceTrial_MissesDoNotAdvance_AndAClosingShotNeedsTwoCadence()
    {
        var trial = new TwinPistolTrialProgress();
        trial.RecordAbility(Start, false, PerkType.TempoShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(1), true, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(2), true, PerkType.ClosingShot, 3).Should().BeFalse("no landed Tempo Shot started the sequence");
        trial.RecordAbility(Start.AddSeconds(3), true, PerkType.TempoShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(4), false, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(5), true, PerkType.ClosingShot, 3).Should().BeFalse("the Lead Shot missed");
        trial.RecordAbility(Start.AddSeconds(6), true, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(7), true, PerkType.ClosingShot, 1).Should().BeFalse("one Cadence is not enough");
        trial.RecordAbility(Start.AddSeconds(8), false, PerkType.ClosingShot, 3).Should().BeFalse("a missed Closing Shot does not count");
        trial.RecordAbility(Start.AddSeconds(9), true, PerkType.ClosingShot, 2).Should().BeTrue();
    }

    [Test]
    public void SequenceTrial_OtherAbilitiesInBetweenDoNotBreakIt()
    {
        var trial = new TwinPistolTrialProgress();
        trial.RecordAbility(Start, true, PerkType.TempoShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(1), true, PerkType.FocusedBurst, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(3), true, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(5), true, PerkType.PairedVolley, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(7), true, PerkType.ClosingShot, 3).Should().BeTrue();
    }

    [Test]
    public void SequenceTrial_OrderMatters_AndANewTempoShotRestartsIt()
    {
        var trial = new TwinPistolTrialProgress();
        trial.RecordAbility(Start, true, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(1), true, PerkType.TempoShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(2), true, PerkType.ClosingShot, 3).Should().BeFalse("Lead Shot came before Tempo Shot");
        trial.RecordAbility(Start.AddSeconds(3), true, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(4), true, PerkType.TempoShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(5), true, PerkType.ClosingShot, 3).Should().BeFalse("the new Tempo Shot needs its own Lead Shot");
    }

    [Test]
    public void SequenceTrial_ExpiresEighteenSecondsAfterTheTempoShot()
    {
        var trial = new TwinPistolTrialProgress();
        trial.RecordAbility(Start, true, PerkType.TempoShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(2), true, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(18), true, PerkType.ClosingShot, 3).Should().BeFalse();

        trial.RecordAbility(Start.AddSeconds(20), true, PerkType.TempoShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(30), true, PerkType.LeadShot, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(37.9), true, PerkType.ClosingShot, 2).Should().BeTrue("just inside the window");
    }
}
