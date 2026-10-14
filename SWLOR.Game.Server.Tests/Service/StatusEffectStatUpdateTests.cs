using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectStatUpdateTests
{
    [Test]
    public void UnstablePressure_TickPenaltyReachesTheCreatureTotalAndClearsOnRemoval()
    {
        var effect = new UnstablePressureStatusEffect();
        var tracker = new CreatureStatusEffect();
        tracker.Add(effect);

        tracker.SetStat(effect, StatType.ForceDamageTakenPercentAdjustment, 5).Should().BeTrue();
        tracker.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(5);
        tracker.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-5);
        tracker.SetStat(effect, StatType.ForceDamageTakenPercentAdjustment, 5).Should().BeFalse(
            "an unchanged value needs no stat refresh");

        tracker.SetStat(effect, StatType.ForceDamageTakenPercentAdjustment, 0);
        tracker.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(0);

        tracker.SetStat(effect, StatType.ForceDamageTakenPercentAdjustment, 5);
        tracker.GetAllTickEffects().Should().ContainSingle().Which.Should().BeSameAs(effect);
        tracker.Remove(effect);
        tracker.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(0,
            "removal must subtract the penalty the creature total actually holds");
        tracker.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(0);
    }

    [Test]
    public void SignalJammer_TickSuppressionCancelsOutsideHasteAndLeavesNoResidue()
    {
        var haste = new Hasten1StatusEffect();
        var jammer = new SignalJammerStatusEffect();
        var tracker = new CreatureStatusEffect();
        tracker.Add(haste);
        tracker.Add(jammer);

        tracker.SetStat(jammer, StatType.AttackDelayReductionPercent, -15);
        tracker.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(0);
        tracker.StatGroup.Stats[StatType.AttackDelayReductionSuppressed].Should().Be(1);

        tracker.SetStat(jammer, StatType.AttackDelayReductionPercent, -40);
        tracker.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(-25);

        tracker.Remove(jammer);
        tracker.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(15,
            "only the outside haste remains once the jammer ends");
        tracker.StatGroup.Stats[StatType.AttackDelayReductionSuppressed].Should().Be(0);
    }

    [Test]
    public void SetStat_UntrackedEffect_ChangesOnlyTheEffect()
    {
        var effect = new UnstablePressureStatusEffect();
        var tracker = new CreatureStatusEffect();

        tracker.SetStat(effect, StatType.ForceDamageTakenPercentAdjustment, 5).Should().BeFalse();

        effect.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(5);
        tracker.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(0);
        tracker.GetAllEffects().Should().BeEmpty();

        tracker.Add(effect);
        tracker.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(5,
            "an application-time value is picked up when the effect is added");
    }
}
