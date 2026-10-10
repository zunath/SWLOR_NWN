using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Feature;

public class MarkedForDeathStatusEffectTests
{
    private const uint Marker = 10;
    private const uint MarkedTarget = 20;
    private const uint Bystander = 30;

    [Test]
    public void Mark_OnlyAmplifiesDamageFromItsSourceByPercent()
    {
        new MarkedForDeathStatusEffect().StatGroup.Stats
            .Where(stat => stat.Value != 0)
            .ToDictionary(stat => stat.Key, stat => stat.Value)
            .Should().BeEquivalentTo(
            new Dictionary<StatType, int>
            {
                [StatType.DamageTakenFromStatusSourcePercentAdjustment] = 50,
            },
            "the bonus must scale with the marker's own hits, not add a flat amount");
    }

    [Test]
    public void Mark_IsSpentByTheMarkersThirdDamagingHit()
    {
        var mark = CreateMark();

        mark.RegisterDamagingHit(Marker).Should().BeFalse();
        mark.RemainingAttacks.Should().Be(2);
        mark.RegisterDamagingHit(Marker).Should().BeFalse();
        mark.RemainingAttacks.Should().Be(1);
        mark.RegisterDamagingHit(Marker).Should().BeTrue("the third hit uses the last charge");
        mark.RemainingAttacks.Should().Be(0);
    }

    [Test]
    public void Mark_IgnoresHitsFromOtherAttackers()
    {
        var mark = CreateMark();

        mark.RegisterDamagingHit(Bystander).Should().BeFalse();
        mark.RegisterDamagingHit(MarkedTarget).Should().BeFalse();

        mark.RemainingAttacks.Should().Be(MarkedForDeathStatusEffect.AttackLimit);
    }

    [Test]
    public void SpentMark_DoesNotReportRemovalAgain()
    {
        var mark = CreateMark();
        for (var hit = 0; hit < MarkedForDeathStatusEffect.AttackLimit; hit++)
            mark.RegisterDamagingHit(Marker);

        mark.RegisterDamagingHit(Marker).Should().BeFalse();
        mark.RemainingAttacks.Should().Be(0);
    }

    [Test]
    public void ReappliedMark_StartsWithFullCharges()
    {
        var spent = CreateMark();
        for (var hit = 0; hit < MarkedForDeathStatusEffect.AttackLimit; hit++)
            spent.RegisterDamagingHit(Marker);

        var reapplied = (MarkedForDeathStatusEffect)spent.Clone();

        reapplied.RemainingAttacks.Should().Be(MarkedForDeathStatusEffect.AttackLimit);
        reapplied.StatGroup.Stats[StatType.DamageTakenFromStatusSourcePercentAdjustment]
            .Should().Be(MarkedForDeathStatusEffect.DamageTakenFromSourcePercent);
    }

    private static MarkedForDeathStatusEffect CreateMark()
    {
        var mark = new MarkedForDeathStatusEffect();
        mark.ApplyEffect(Marker, MarkedTarget, 2);
        return mark;
    }
}
