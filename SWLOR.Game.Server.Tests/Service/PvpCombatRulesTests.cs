using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PvpCombatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

public class PvpCombatRulesTests
{
    [TestCase(0, 0)]
    [TestCase(119.9, 0)]
    [TestCase(120, 10)]
    [TestCase(149, 10)]
    [TestCase(150, 20)]
    [TestCase(180, 30)]
    [TestCase(210, 45)]
    [TestCase(240, 60)]
    [TestCase(3600, 60)]
    public void HealingReduction_StepsFromTwoMinutesAndStopsAtSixtyPercent(double seconds, int expected)
    {
        PvpCombatRules.GetStepPercent(PvpCombatRules.HealingReductionSteps, seconds).Should().Be(expected);
    }

    [TestCase(0, 0)]
    [TestCase(179.9, 0)]
    [TestCase(180, 20)]
    [TestCase(210, 40)]
    [TestCase(240, 60)]
    [TestCase(270, 80)]
    [TestCase(300, 100)]
    [TestCase(3600, 100)]
    public void DamageIncrease_StepsFromThreeMinutesUpToDouble(double seconds, int expected)
    {
        PvpCombatRules.GetStepPercent(PvpCombatRules.DamageIncreaseSteps, seconds).Should().Be(expected);
    }

    [TestCase(119, -1)]
    [TestCase(120, 0)]
    [TestCase(239, 3)]
    [TestCase(240, 4)]
    public void StepIndex_IsMinusOneBeforeTheFirstStep(double seconds, int expected)
    {
        PvpCombatRules.GetStepIndex(PvpCombatRules.HealingReductionSteps, seconds).Should().Be(expected);
    }

    [TestCase(100, 0, 100)]
    [TestCase(100, 10, 90)]
    [TestCase(100, 60, 40)]
    [TestCase(7, 10, 6)]
    [TestCase(1, 60, 0)]
    [TestCase(0, 60, 0)]
    public void ReduceHealing_RoundsTheCutUp(int amount, int percent, int expected)
    {
        PvpCombatRules.ReduceHealing(amount, percent).Should().Be(expected);
    }

    [TestCase(100, 0, 100)]
    [TestCase(100, 20, 120)]
    [TestCase(100, 100, 200)]
    [TestCase(7, 20, 9)]
    [TestCase(0, 100, 0)]
    public void IncreaseDamage_RoundsTheIncreaseUp(int damage, int percent, int expected)
    {
        PvpCombatRules.IncreaseDamage(damage, percent).Should().Be(expected);
    }

    [TestCase(30, 15)]
    [TestCase(5, 3)]
    [TestCase(1, 1)]
    [TestCase(0, 0)]
    [TestCase(-1, -1)]
    public void HardCrowdControlAgainstPlayers_LastsHalfAsLongButAtLeastOneTick(int ticks, int expected)
    {
        PvpCombatRules.ShortenHardCrowdControlTicks(ticks).Should().Be(expected);
    }

    [Test]
    public void HardCrowdControlAgainstPlayers_IsHalvedAfterTheThirtySecondBudget()
    {
        const StatusEffectCategory hardControl = StatusEffectCategory.Control | StatusEffectCategory.HardCrowdControl;

        StatusEffect.ClampHardCrowdControlDurationTicks(hardControl, 60, 1f, isPlayerAgainstPlayer: true)
            .Should().Be(15, "the 30-second budget applies first, then players take half");
        StatusEffect.ClampHardCrowdControlDurationTicks(hardControl, 10, 1f, isPlayerAgainstPlayer: true).Should().Be(5);
        StatusEffect.ClampHardCrowdControlDurationTicks(hardControl, 10, 1f).Should().Be(10, "NPC targets keep the full duration");
        StatusEffect.ClampHardCrowdControlDurationTicks(StatusEffectCategory.Control, 10, 1f, isPlayerAgainstPlayer: true)
            .Should().Be(10, "soft control is not shortened");
        StatusEffect.ClampHardCrowdControlDurationTicks(hardControl, 60, 1f, isSelfApplied: true, isPlayerAgainstPlayer: true)
            .Should().Be(60, "self-applied control such as subdual keeps its duration");
    }
}
