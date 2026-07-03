using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatFormulaTests
{
    [Test]
    public void CalculateHitRate_ClampsBetweenMinimumAndMaximum()
    {
        CombatFormula.CalculateHitRate(0, 200, 0).Should().Be(20);
        CombatFormula.CalculateHitRate(300, 0, 0).Should().Be(95);
    }

    [Test]
    public void CalculateHitRate_AppliesAccuracyEvasionAndModifier()
    {
        var hitRate = CombatFormula.CalculateHitRate(10, 8, 5);

        hitRate.Should().Be(81);
    }

    [Test]
    public void CalculateCriticalRate_ClampsBetweenBaseAndMaximum()
    {
        CombatFormula.CalculateCriticalRate(0, 100, 0, -100).Should().Be(5);
        CombatFormula.CalculateCriticalRate(100, 0, 500, 100).Should().Be(50);
    }

    [Test]
    public void CalculateCriticalRate_AppliesStatsSkillAndModifier()
    {
        var criticalRate = CombatFormula.CalculateCriticalRate(20, 10, 25, 2);

        criticalRate.Should().Be(11);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_AppliesOffhandBeforeGlobalReduction()
    {
        var delay = CombatFormula.CalculateAttackDelayMilliseconds(210, 210, 45, 30);

        delay.Should().Be(2310);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_CapsPositiveGlobalAndOffhandReductions()
    {
        var delay = CombatFormula.CalculateAttackDelayMilliseconds(210, 210, 90, 90);

        delay.Should().Be(1750);
    }

    [Test]
    public void CalculateAttacksPerSwing_CarriesFractionalAttackDebt()
    {
        var firstSwingAttacks = CombatFormula.CalculateAttacksPerSwing(1000, 0f, out var attackDebt);
        var secondSwingAttacks = CombatFormula.CalculateAttacksPerSwing(1000, attackDebt, out var updatedAttackDebt);

        firstSwingAttacks.Should().Be(1);
        attackDebt.Should().BeApproximately(0.75f, 0.01f);
        secondSwingAttacks.Should().Be(2);
        updatedAttackDebt.Should().BeApproximately(0.5f, 0.01f);
    }
}
