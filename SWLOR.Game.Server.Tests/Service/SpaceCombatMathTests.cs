using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The space combat rules (plan sections 5, 8 and 9). The expected numbers are worked out by hand from the same
/// formulas the combat simulator's SpaceShotMath uses, so the two stay in step.
/// </summary>
public class SpaceCombatMathTests
{
    /// <summary>Energy weapons add AGI x 3 to accuracy; launchers add PER x 3.</summary>
    [Test]
    public void Accuracy_UsesAgiForEnergyAndPerForLaunchers()
    {
        SpaceCombatMath.Accuracy(10, 20, agi: 14, per: 12, SpaceWeaponFamily.Energy).Should().Be(72);
        SpaceCombatMath.Accuracy(10, 20, agi: 14, per: 12, SpaceWeaponFamily.Launcher).Should().Be(66);
    }

    [Test]
    public void Evasion_AddsAgiTimesThree()
    {
        SpaceCombatMath.Evasion(5, 12).Should().Be(41);
    }

    [Test]
    public void DamageAttribute_IsPerForEnergyAndMgtForLaunchers()
    {
        SpaceCombatMath.DamageAttribute(SpaceWeaponFamily.Energy, per: 14, mgt: 20).Should().Be(14);
        SpaceCombatMath.DamageAttribute(SpaceWeaponFamily.Launcher, per: 14, mgt: 20).Should().Be(20);
    }

    [Test]
    public void AttackAndDefense_AddTheirParts()
    {
        SpaceCombatMath.Attack(10, 20, 15).Should().Be(45);
        SpaceCombatMath.Defense(4, 12).Should().Be(16);
    }

    /// <summary>Positive steps mean the weapon is larger than the target; ignoring size zeroes them.</summary>
    [Test]
    public void SizeSteps_CountBothDirectionsAndHonorIgnoreSize()
    {
        SpaceCombatMath.SizeSteps(ShipSize.Large, ShipSize.Small, false).Should().Be(2);
        SpaceCombatMath.SizeSteps(ShipSize.Small, ShipSize.Large, false).Should().Be(-2);
        SpaceCombatMath.SizeSteps(ShipSize.Medium, ShipSize.Medium, false).Should().Be(0);
        SpaceCombatMath.SizeSteps(ShipSize.Capital, ShipSize.Small, true).Should().Be(0);
    }

    /// <summary>
    /// Base 75 + floor((accuracy - evasion) / 2) - 10 per step the weapon is above the target, limited to 20-95
    /// after the penalty.
    /// </summary>
    [TestCase(60, 40, ShipSize.Medium, ShipSize.Medium, false, 85)]
    [TestCase(60, 40, ShipSize.Capital, ShipSize.Small, false, 55)]
    [TestCase(60, 40, ShipSize.Capital, ShipSize.Small, true, 85)]
    [TestCase(60, 40, ShipSize.Small, ShipSize.Capital, false, 85)]
    [TestCase(100, 0, ShipSize.Capital, ShipSize.Small, false, 95)]
    [TestCase(200, 0, ShipSize.Small, ShipSize.Small, false, 95)]
    [TestCase(0, 200, ShipSize.Small, ShipSize.Small, false, 20)]
    public void HitChance_AppliesSizePenaltyBeforeTheLimits(
        int accuracy, int evasion, ShipSize weapon, ShipSize target, bool ignoreSize, int expected)
    {
        SpaceCombatMath.HitChance(accuracy, evasion, weapon, target, ignoreSize).Should().Be(expected);
    }

    /// <summary>A smaller weapon loses 12% per step, compounding; a larger or equal one loses nothing.</summary>
    [Test]
    public void SizeDamageMultiplier_CompoundsPerStepBelowTheTarget()
    {
        SpaceCombatMath.SizeDamageMultiplier(ShipSize.Small, ShipSize.Capital, false).Should().BeApproximately(0.681472, 1e-9);
        SpaceCombatMath.SizeDamageMultiplier(ShipSize.Small, ShipSize.Medium, false).Should().BeApproximately(0.88, 1e-9);
        SpaceCombatMath.SizeDamageMultiplier(ShipSize.Capital, ShipSize.Small, false).Should().Be(1.0);
        SpaceCombatMath.SizeDamageMultiplier(ShipSize.Small, ShipSize.Capital, true).Should().Be(1.0);
    }

    [TestCase(-5, 1)]
    [TestCase(0, 1)]
    [TestCase(5, 1)]
    [TestCase(10, 2)]
    [TestCase(45, 5)]
    [TestCase(50, 6)]
    [TestCase(100, 6)]
    public void DeltaCap_FollowsRankAndIsLimitedToSix(int rank, int expected)
    {
        SpaceCombatMath.DeltaCap(rank).Should().Be(expected);
    }

    /// <summary>5 + rank / 10 + (PER - VIT) / 5 limited to 0-3, then the modifier, limited to 5-50.</summary>
    [TestCase(10, 20, 0, 0, 5)]
    [TestCase(25, 10, 0, 0, 8)]
    [TestCase(25, 10, 40, 0, 12)]
    [TestCase(25, 10, 40, 100, 50)]
    [TestCase(10, 10, 0, -100, 5)]
    public void CriticalRate_UsesTheGroundFormulaAndItsLimits(int per, int vit, int rank, int modifier, int expected)
    {
        SpaceCombatMath.CriticalRate(per, vit, rank, modifier).Should().Be(expected);
    }

    /// <summary>
    /// Equal attributes give no stat delta, so the range is DMG x attack / defense (ratio limited to 3.625), with a
    /// 70% floor. A critical (rating 2) fixes the floor at the maximum and raises the maximum by 25%.
    /// </summary>
    [Test]
    public void DamageRange_ScalesWithTheAttackToDefenseRatio()
    {
        SpaceCombatMath.DamageRange(100, 20, 10, 50, 10, false, 0).Should().Be((28, 40));
        SpaceCombatMath.DamageRange(100, 20, 10, 50, 10, true, 0).Should().Be((40, 50));
        SpaceCombatMath.DamageRange(400, 20, 10, 50, 10, false, 0).Should().Be((50, 72));
    }

    /// <summary>The attribute delta is capped by the module rank's delta cap: -cap up to 8 + cap.</summary>
    [Test]
    public void DamageRange_CapsTheAttributeDeltaByRank()
    {
        SpaceCombatMath.DamageRange(50, 20, 60, 50, 10, false, 0).Should().Be((20, 29));
        SpaceCombatMath.DamageRange(50, 20, 60, 50, 10, false, 45).Should().Be((23, 33));
        SpaceCombatMath.DamageRange(50, 20, 10, 50, 60, false, 0).Should().Be((13, 19));
    }

    [TestCase(0, 1.0)]
    [TestCase(25, 0.75)]
    [TestCase(-50, 1.5)]
    [TestCase(150, 0.0)]
    [TestCase(-150, 2.0)]
    public void ResistanceMultiplier_IsLimitedToPlusOrMinusOneHundredPercent(int resistance, double expected)
    {
        SpaceCombatMath.ResistanceMultiplier(resistance).Should().BeApproximately(expected, 1e-9);
    }

    [Test]
    public void ApplyToPools_ShieldAbsorbsThenHullTakesTheRest()
    {
        double shield = 50, hull = 100;
        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 30, false, 1.0).Should().BeApproximately(30, 1e-9);
        (shield, hull).Should().Be((20, 100));

        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 30, false, 1.0).Should().BeApproximately(30, 1e-9);
        (shield, hull).Should().Be((0, 90));

        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 25, false, 1.0).Should().BeApproximately(25, 1e-9);
        (shield, hull).Should().Be((0, 65));
    }

    [Test]
    public void ApplyToPools_BypassSendsEverythingToTheHull()
    {
        double shield = 50, hull = 100;
        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 30, true, 1.0).Should().BeApproximately(30, 1e-9);
        (shield, hull).Should().Be((50, 70));
    }

    /// <summary>With a 1.5 multiplier the shield loses 1.5 points per point; overflow reaches the hull at 1x.</summary>
    [Test]
    public void ApplyToPools_ShieldMultiplierScalesWhatTheShieldLoses()
    {
        double shield = 60, hull = 100;
        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 20, false, 1.5).Should().BeApproximately(30, 1e-9);
        (shield, hull).Should().Be((30, 100));

        // 15 shield absorbs 10 of the 20 damage; the other 10 goes to the hull.
        shield = 15;
        hull = 100;
        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 20, false, 1.5).Should().BeApproximately(25, 1e-9);
        shield.Should().Be(0);
        hull.Should().BeApproximately(90, 1e-9);
    }

    [Test]
    public void ApplyToPools_StopsAtTheLastHullPointAndIgnoresDeadShips()
    {
        double shield = 0, hull = 10;
        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 50, false, 1.0).Should().Be(10);
        hull.Should().Be(0);

        shield = 5;
        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 50, false, 1.0).Should().Be(0);
        shield.Should().Be(5);

        hull = 10;
        SpaceCombatMath.ApplyToPools(ref shield, ref hull, 0, false, 1.0).Should().Be(0);
        (shield, hull).Should().Be((5, 10));
    }

    /// <summary>Max shield x size rate x (1 + 2% per WIL above 10) x (1 + bonus).</summary>
    [TestCase(ShipSize.Small, 10, 0, 1.0)]
    [TestCase(ShipSize.Medium, 10, 0, 1.1)]
    [TestCase(ShipSize.Large, 10, 0, 1.2)]
    [TestCase(ShipSize.Capital, 10, 0, 1.3)]
    [TestCase(ShipSize.Small, 25, 0, 1.3)]
    [TestCase(ShipSize.Medium, 25, 0, 1.43)]
    [TestCase(ShipSize.Large, 25, 0, 1.56)]
    [TestCase(ShipSize.Capital, 25, 0, 1.69)]
    [TestCase(ShipSize.Small, 10, 50, 1.5)]
    [TestCase(ShipSize.Invalid, 10, 0, 1.0)]
    public void ShieldRegenPerSecond_ScalesWithSizeWilAndBonus(ShipSize size, int wil, int bonusPercent, double expected)
    {
        SpaceCombatMath.ShieldRegenPerSecond(200, size, wil, bonusPercent).Should().BeApproximately(expected, 1e-9);
    }

    [Test]
    public void CapacitorRegenPerSecond_AddsFiveHundredthsPerWil()
    {
        SpaceCombatMath.CapacitorRegenPerSecond(1.0, 10).Should().BeApproximately(1.5, 1e-9);
        SpaceCombatMath.CapacitorRegenPerSecond(2.0, 25).Should().BeApproximately(3.25, 1e-9);
        SpaceCombatMath.CapacitorRegenPerSecond(1.0, 0).Should().BeApproximately(1.0, 1e-9);
    }
}
