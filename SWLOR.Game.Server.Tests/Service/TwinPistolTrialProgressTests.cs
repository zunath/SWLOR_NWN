using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

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
    public void MovementTrial_RequiresBothSuccessfulBuilders_AndAChargedSuccessfulSpender()
    {
        var trial = new TwinPistolTrialProgress();
        var slide = new TwinPistolAbilitySpec { IsCrossfire = true, GeneratesMomentum = true, Movement = TwinPistolMovementType.Retreat };
        var rush = slide with { Movement = TwinPistolMovementType.FreeDestination };
        var grapple = new TwinPistolAbilitySpec { IsCrossfire = true, SpendsMomentum = true, Movement = TwinPistolMovementType.Approach };
        trial.RecordAbility(Start, false, 4, slide, 0).Should().BeFalse();
        trial.RecordAbility(Start, true, 1, slide, 0).Should().BeFalse();
        trial.RecordAbility(Start, true, 4, slide, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(2), true, 4, slide, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(4), true, 4, rush, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(6), false, 4, grapple, 2).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(7), true, 4, grapple, 1).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(8), true, 0, grapple, 2).Should().BeTrue();
    }

    [Test]
    public void MovementTrial_ExpiredBuildersCannotValidateALaterSpender()
    {
        var trial = new TwinPistolTrialProgress();
        var slide = new TwinPistolAbilitySpec { IsCrossfire = true, GeneratesMomentum = true, Movement = TwinPistolMovementType.Retreat };
        var rush = slide with { Movement = TwinPistolMovementType.FreeDestination };
        var grapple = slide with { GeneratesMomentum = false, SpendsMomentum = true, Movement = TwinPistolMovementType.Approach };
        trial.RecordAbility(Start, true, 4, rush, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(2), true, 4, slide, 0).Should().BeFalse();
        trial.RecordAbility(Start.AddSeconds(18), true, 4, grapple, 3).Should().BeFalse();
    }
}
