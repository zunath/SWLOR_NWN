using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The pilot's percent and flat stats in the space formulas. Each rule matches what the ground does with the
/// same stat (Stat.GetAccuracy and siblings, Combat.ApplyCriticalDamageModifier, Combat.ApplyDamageDealtModifiers
/// and Combat.ApplyDamageTakenModifiers).
/// </summary>
public class SpacePilotStatMathTests
{
    /// <summary>The flat stat joins the rating, then the percent stat scales the total, as on the ground.</summary>
    [TestCase(100, 10, 20, 132)]
    [TestCase(100, 0, 0, 100)]
    [TestCase(100, 0, -20, 80)]
    [TestCase(100, -10, -10, 81)]
    [TestCase(10, 0, 15, 12)]
    public void AdjustRating_AddsTheFlatStatThenScalesByThePercentStat(int rating, int flat, int percent, int expected)
    {
        SpaceCombatMath.AdjustRating(rating, flat, percent).Should().Be(expected);
    }

    [Test]
    public void AdjustRating_NeverFallsBelowOne()
    {
        SpaceCombatMath.AdjustRating(5, -50, 0).Should().Be(1);
        SpaceCombatMath.AdjustRating(0, 0, -90).Should().Be(1);
    }

    /// <summary>A rating built from the ground's own formula and scaled the same way gives the same number.</summary>
    [Test]
    public void AdjustRating_MatchesTheGroundAccuracyOrder()
    {
        const int level = 20;
        const int stat = 14;
        const int flat = 6;
        const int percent = 12;

        var groundBase = Stat.GetAccuracy(level, stat, flat);
        var expected = Math.Max(1, Stat.ApplyPercentAdjustment(groundBase, percent));

        SpaceCombatMath.AdjustRating(Stat.GetAccuracy(level, stat, 0), flat, percent).Should().Be(expected);
    }

    [TestCase(100, true, 15, 115)]
    [TestCase(100, true, 0, 100)]
    [TestCase(100, false, 50, 100)]
    [TestCase(100, true, 500, 300)]
    [TestCase(100, true, -20, 80)]
    [TestCase(0, true, 50, 0)]
    public void CriticalDamage_AddsThePercentOnlyToCriticalHitsAndLimitsItLikeTheGround(int damage, bool critical, int percent, int expected)
    {
        SpaceCombatMath.CriticalDamage(damage, critical, percent).Should().Be(expected);
    }

    [TestCase(100, 100, 70, 15, 15)]
    [TestCase(70, 100, 70, 15, 15)]
    [TestCase(69, 100, 70, 15, 0)]
    [TestCase(100, 100, 0, 15, 0)]
    [TestCase(100, 100, 70, 0, 0)]
    [TestCase(100, 0, 70, 15, 0)]
    public void HighHullCriticalDamageAdjustment_NeedsTheTargetHullAtOrAboveTheThreshold(
        int hull, int maxHull, int threshold, int adjustment, int expected)
    {
        SpaceCombatMath.HighHullCriticalDamageAdjustment(hull, maxHull, threshold, adjustment).Should().Be(expected);
    }

    [Test]
    public void ShapeDamagePercent_UsesTheAreaStatOnlyForAreaAbilities()
    {
        SpaceCombatMath.ShapeDamagePercent(isAbility: false, isArea: false, singleTargetPercent: -10, areaPercent: 10).Should().Be(-10);
        SpaceCombatMath.ShapeDamagePercent(isAbility: true, isArea: false, singleTargetPercent: -10, areaPercent: 10).Should().Be(-10);
        SpaceCombatMath.ShapeDamagePercent(isAbility: true, isArea: true, singleTargetPercent: -10, areaPercent: 10).Should().Be(10);
        SpaceCombatMath.ShapeDamagePercent(isAbility: false, isArea: true, singleTargetPercent: -10, areaPercent: 10)
            .Should().Be(-10, "a weapon cycle is never an area ability");
    }

    [Test]
    public void ApplyDamageDealt_AppliesTheDealtPercentThenTheShapePercent()
    {
        SpaceCombatMath.ApplyDamageDealt(100, 10, 10).Should().Be(121);
        SpaceCombatMath.ApplyDamageDealt(100, 0, 0).Should().Be(100);
        SpaceCombatMath.ApplyDamageDealt(100, -20, 0).Should().Be(80);
    }

    [Test]
    public void ApplyLeadershipReduction_ReducesByThePercent()
    {
        SpaceCombatMath.ApplyLeadershipReduction(100, -6).Should().Be(94);
        SpaceCombatMath.ApplyLeadershipReduction(100, 0).Should().Be(100);
    }

    [Test]
    public void ApplyDamageTaken_UsesTheGroundReductionLimits()
    {
        SpaceCombatMath.ApplyDamageTaken(100, 0).Should().Be(100);
        SpaceCombatMath.ApplyDamageTaken(100, -50).Should().Be(50);
        SpaceCombatMath.ApplyDamageTaken(100, 25).Should().Be(125);
        SpaceCombatMath.ApplyDamageTaken(100, -100).Should().Be(15, "the combined reduction is capped at 85%");
        SpaceCombatMath.ApplyDamageTaken(1, -50).Should().Be(1, "a hit always does at least 1");
    }

    [TestCase(300, 240, 1000, 25, true)]
    [TestCase(260, 251, 1000, 25, false)]
    [TestCase(260, 249, 1000, 25, true)]
    [TestCase(249, 100, 1000, 25, false)]
    [TestCase(250, 249, 1000, 25, true)]
    [TestCase(500, 0, 1000, 25, false)]
    [TestCase(500, 100, 0, 25, false)]
    [TestCase(500, 100, 1000, 0, false)]
    public void CrossedHullThreshold_FiresOnlyWhenDamageTakesHullBelowThePercent(
        int before, int after, int max, int percent, bool expected)
    {
        SpaceCombatMath.CrossedHullThreshold(before, after, max, percent).Should().Be(expected);
    }

    [Test]
    public void HullEvent_CrossedBelow_UsesTheEventsHullValues()
    {
        var hullEvent = new SpaceHullEvent { HullBefore = 300, HullAfter = 240, MaxHull = 1000 };

        hullEvent.CrossedBelow(25).Should().BeTrue();
        hullEvent.CrossedBelow(20).Should().BeFalse();
        hullEvent.CrossedBelow(50).Should().BeFalse("it was already below 50%");
    }

    /// <summary>Repairs generate half their restored points as threat, like ground healing.</summary>
    [TestCase(100, 50)]
    [TestCase(1, 0)]
    [TestCase(0, 0)]
    [TestCase(-5, 0)]
    public void RepairEnmity_IsHalfOfWhatWasRestored(int restored, int expected)
    {
        SpaceCombatMath.RepairEnmity(restored).Should().Be(expected);
    }

    [Test]
    public void RepairEnmity_MatchesTheGroundHealingThreatRule()
    {
        SpaceCombatMath.RepairEnmity(80).Should().Be(Enmity.CalculateHealingEnmity(80, 20, 100));
    }

    /// <summary>Weapon threat uses the damage enmity stat, so a threat stance doubles it.</summary>
    [Test]
    public void DamageThreat_IncludesTheDamageEnmityAdjustment()
    {
        Enmity.CalculateDamageEnmity(40, 100).Should().Be(80);
        Enmity.CalculateDamageEnmity(40, 0).Should().Be(40);
    }

    [Test]
    public void Blindness_MissesHalfOfAHitAttackersShots()
    {
        SpaceCombatMath.BlindMissChance.Should().Be(50);
    }
}
