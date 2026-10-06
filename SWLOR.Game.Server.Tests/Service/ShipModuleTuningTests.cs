using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipModuleTuningTests
{
    private static readonly ShipFittingCatalog Catalog = ShipFittingCatalog.Default;

    [Test]
    public void Refinement_ChangesExactlyOneDimensionAndDoesNotMutateTheSource()
    {
        var module = Catalog.Modules["tracking_laser"];
        var baseline = Catalog.GetVariant(module.Id);
        var result = ShipModuleTuning.Refine(module, baseline, ShipQualityDimension.Output, 100);
        result.Output.Should().BeApproximately(12.96, 1e-9);
        result.Cycle.Should().Be(baseline.Cycle);
        result.Capacitor.Should().Be(baseline.Capacitor);
        result.Tracking.Should().Be(baseline.Tracking);
        result.Power.Should().Be(baseline.Power);
        baseline.Output.Should().Be(12);
        ShipModuleTuning.Refine(module, baseline, ShipQualityDimension.Output, 100).Should().Be(result);
    }

    [TestCase(ShipQualityDimension.Output | ShipQualityDimension.Tracking, 100)]
    [TestCase(ShipQualityDimension.CycleDuration, 100)]
    [TestCase(ShipQualityDimension.Output, 101)]
    [TestCase(ShipQualityDimension.None, -1)]
    public void InvalidOrCombinedQualityDimensions_AreRejected(ShipQualityDimension dimension, int quality)
    {
        Action refine = () => ShipModuleTuning.Refine(Catalog.Modules["tracking_laser"],
            Catalog.GetVariant("tracking_laser"), dimension, quality);
        refine.Should().Throw<ArgumentException>();
    }

    [Test]
    public void NegativeCalibrationTradeoff_RemainsWithMasteryAndTemporaryEffects()
    {
        ShipModuleTuning.Output(100, 85, 0.9, 0.1, new[] { 0.1, 0.2, 0.9 })
            .Should().BeApproximately(145, 1e-9);
        ShipModuleTuning.Output(100, 115, 0.3).Should().BeApproximately(140, 1e-9);
    }

    [Test]
    public void TemporaryOutput_UsesHighestSource()
    {
        ShipModuleTuning.Output(100, 100, temporaryBonuses: new[] { 0.1, 0.2 })
            .Should().BeApproximately(120, 1e-9);
    }

    [TestCase(4, 0.75, 0.4, 0, 3)]
    [TestCase(5, 0.75, 0.08, 0, 4)]
    [TestCase(4, 1.2, 0.25, 0, 4)]
    [TestCase(4, 1.25, 0.9, 0.2, 5)]
    [TestCase(0, 1, 0.5, 0, 0)]
    public void CostDiscounts_ShareOneCapAndPreserveDemand(int standard, double calibration,
        double discount, double demand, int expected)
    {
        ShipModuleTuning.CapacitorCost(standard, calibration, discount, demand).Should().Be(expected);
    }

    [Test]
    public void EnergyTransfer_RejectsOutputAndActivationCostQuality()
    {
        foreach (var dimension in new[] { ShipQualityDimension.Output, ShipQualityDimension.ActivationCost })
        {
            Action refine = () => ShipModuleTuning.Refine(Catalog.Modules["transfer_projector"],
                Catalog.GetVariant("transfer_projector"), dimension, 100);
            refine.Should().Throw<ArgumentException>();
        }
    }

    [Test]
    public void RefitRoundTrips_PreserveUnclampedDamageAndExpenditure()
    {
        var original = new ShipStatus {
            MaxHull = 140, Hull = 20, MaxShield = 100, Shield = 10, MaxCapacitor = 80, Capacitor = 5 };
        var deficits = ShipResourceDeficits.Capture(original);
        deficits.Apply(100, 70, 60).Should().Be((1, 0, 0));
        deficits.Apply(140, 100, 80).Should().Be((20, 10, 5));
        original.Hull.Should().Be(20);
    }

    [Test]
    public void CombatMastery_IsBoundedByRelevantSkillAndDeclaredAttributeBudget()
    {
        var module = Catalog.Modules["tracking_laser"];
        var refined = ShipModuleTuning.Refine(module, Catalog.GetVariant(module.Id), ShipQualityDimension.Output, 100);
        ShipCombatMath.WeaponOutput(module, refined, 50, 26, 0.05).Should().BeApproximately(15.24, 1e-9);
        ShipCombatMath.WeaponOutput(module, refined, 50, 100, 0.05).Should().BeApproximately(15.24, 1e-9);
        var repair = Catalog.Modules["hull_repair"];
        ShipCombatMath.RecoveryOutput(repair, Catalog.GetVariant(repair.Id), 50)
            .Should().BeApproximately(repair.Output * 1.10, 1e-9);
    }

    [Test]
    public void LargeWeaponTracking_PreservesTheSmallTargetDisadvantage()
    {
        double Chance(double tracking, double resolution) =>
            ShipCombatMath.HitChance(50, 40, 26, 26, 0, 0, tracking, 38, resolution, 1.296);
        Chance(110 * 1.12, 40).Should().BeGreaterThan(Chance(35 * 1.12, 140) * 2);
        ShipCombatMath.HitChance(50, 0, 26, 10, 1, 0, 200, 240, 40, 0.6).Should().Be(0.95);
        ShipCombatMath.HitChance(0, 50, 10, 26, 0, 1, 0, 38, 140, 1.3).Should().Be(0.10);
    }

    [Test]
    public void TemporaryGroundAttributeSpike_DoesNotExpandShipHitBudget()
    {
        var ordinary = ShipCombatMath.HitChance(50, 40, 26, 26, 0, 0, 110, 38, 40, 1.296);
        ShipCombatMath.HitChance(50, 40, 100, 100, 0, 0, 110, 38, 40, 1.296).Should().Be(ordinary);
    }
}
