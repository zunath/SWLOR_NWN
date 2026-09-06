using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectStatConsumptionTests
{
    [TestCase(1, 8)]
    [TestCase(2, 14)]
    public void EvasiveChallenge_ConsumesItsRefundWithoutRemovingEvasion(int rank, int evasion)
    {
        IStatusEffect effect = rank == 1
            ? new EvasiveChallenge1SelfStatusEffect()
            : new EvasiveChallenge2SelfStatusEffect();
        var tracker = new CreatureStatusEffect();
        tracker.Add(effect);

        tracker.ConsumeStat(StatType.AvoidedAttackSingleStaminaRestore);
        tracker.ConsumeStat(StatType.AvoidedAttackSingleStaminaRestore);

        tracker.GetAllEffects().Should().ContainSingle().Which.Should().BeSameAs(effect);
        tracker.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(evasion);
        tracker.StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore].Should().Be(0);
        effect.IsFlaggedForRemoval.Should().BeFalse();

        tracker.Remove(effect);
        tracker.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(0);
        tracker.StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore].Should().Be(0);

        tracker.Add(rank == 1 ? new EvasiveChallenge1SelfStatusEffect() : new EvasiveChallenge2SelfStatusEffect());
        tracker.StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore].Should().Be(1,
            "a fresh cast must restore the one-use refund");
    }
}
