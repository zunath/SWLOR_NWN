using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Feature;

public class ImmobilizedStatusEffectTests
{
    [Test]
    public void ImmobilizedStatusEffect_DisablesMovementThroughStats()
    {
        var immobilized = new ImmobilizedStatusEffect();

        immobilized.StatGroup.Stats[StatType.MovementSpeedDisabled].Should().Be(1);
        immobilized.StatGroup.Stats[StatType.MovementSpeedPercentAdjustment].Should().Be(0);
    }

    [Test]
    public void MovementSpeedDisabled_IsNonBeneficialStatType()
    {
        Stat.GetStatTypeCategory(StatType.MovementSpeedDisabled).Should().Be(StatTypeCategory.NonBeneficial);
    }
}
