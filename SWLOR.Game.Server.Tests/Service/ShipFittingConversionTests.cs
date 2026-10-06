using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipFittingConversionTests
{
    private static ShipStatus Original() => new() {
        ItemTag = "ShipDeedLightFreighter", MaxHull = 50, Hull = 23,
        MaxShield = 50, Shield = 18, MaxCapacitor = 40, Capacitor = 11 };
    private static ShipStatus.ShipStatusModule Legacy(string tag, string id, int grade = 0) => new() {
        ItemTag = tag, ItemInstanceId = id, SerializedItem = "saved:" + id, ModuleBonus = grade,
        RecastTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

    [Test]
    public void Conversion_PreservesIdentityCooldownDamageAndSourceWithoutMutatingInput()
    {
        var original = Original();
        original.HighPowerModules[1] = Legacy("hull_rep_b", "repair", 70);
        original.FittingPowerUsed = 999;
        var converted = ShipFittingConversion.Convert(original);
        original.FittingVersion.Should().Be(0);
        original.HighPowerModules.Should().ContainKey(1);
        original.LowPowerModules.Should().BeEmpty();
        converted.HighPowerModules.Should().BeEmpty();
        var repair = converted.LowPowerModules[1];
        repair.ItemInstanceId.Should().Be("repair");
        repair.Design.Should().Be("hull_repair");
        repair.Quality.Should().Be(100);
        repair.ModuleBonus.Should().Be(70);
        repair.OriginalSerializedItem.Should().Be("saved:repair");
        repair.RecastTime.Should().Be(original.HighPowerModules[1].RecastTime);
        converted.FittingPowerUsed.Should().Be(12);
        converted.ResourceDeficits.Should().Be(new ShipResourceDeficits(27, 32, 29));
        converted.Hull.Should().Be(113);
        converted.Shield.Should().Be(converted.MaxShield - 32);
        converted.Capacitor.Should().Be(converted.MaxCapacitor - 29);
        ShipFittingConversion.Convert(converted).Should().BeSameAs(converted);
    }

    [Test]
    public void MissingItemIdentities_AreStablePerShipAndCannotCollideAcrossShips()
    {
        var original = Original();
        original.LowPowerModules[1] = Legacy("hull_rep_b", null);
        var first = ShipFittingConversion.Convert(original, sourceIdentity: "ship-one");
        var retry = ShipFittingConversion.Convert(original, sourceIdentity: "ship-one");
        var other = ShipFittingConversion.Convert(original, sourceIdentity: "ship-two");
        first.LowPowerModules[1].ItemInstanceId.Should().Be(retry.LowPowerModules[1].ItemInstanceId);
        first.LowPowerModules[1].ItemInstanceId.Should().NotBe(other.LowPowerModules[1].ItemInstanceId);
        Guid.TryParse(first.LowPowerModules[1].ItemInstanceId, out _).Should().BeTrue();
        var missingOwner = () => ShipFittingConversion.Convert(original);
        missingOwner.Should().Throw<ArgumentException>();
    }

    [Test]
    public void DisplacedAndUnmappedItems_RemainInRecoveryExactlyOnce()
    {
        var original = Original();
        original.HighPowerModules[1] = Legacy("storm_cann", "heavy");
        original.LowPowerModules[1] = Legacy("unknown_fitting", "unknown");
        var converted = ShipFittingConversion.Convert(original);
        converted.RefitRecovery.Keys.Should().BeEquivalentTo("heavy", "unknown");
        converted.RefitRecovery["heavy"].SerializedItem.Should().Be("saved:heavy");
        converted.RefitRecoveryReasons["unknown"].Should().Contain("Unmapped");
        converted.HighPowerModules.Should().BeEmpty();
        converted.LowPowerModules.Should().BeEmpty();
        ShipFittingConversion.Convert(converted).RefitRecovery.Should().HaveCount(2);
    }

    [Test]
    public void DuplicatePersistedItemIdentity_RejectsConversionBeforePersistence()
    {
        var original = Original();
        original.LowPowerModules[1] = Legacy("hull_rep_b", "same");
        original.HighPowerModules[1] = Legacy("com_laser_b", "same");
        var act = () => ShipFittingConversion.Convert(original);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate*");
        original.FittingVersion.Should().Be(0);
        original.RefitRecovery.Should().BeEmpty();
    }

    [Test]
    public void AlreadyTypedEquipment_RetainsItsCalibrationAndQuality()
    {
        var original = Original();
        var repair = Legacy("hull_repair", "typed");
        repair.Design = "hull_repair"; repair.Calibration = "Compact";
        repair.QualityDimension = ShipQualityDimension.Output; repair.Quality = 35;
        original.LowPowerModules[1] = repair;
        var result = ShipFittingConversion.Convert(original);
        result.LowPowerModules[1].Calibration.Should().Be("Compact");
        result.LowPowerModules[1].Quality.Should().Be(35);
        result.FittingPowerUsed.Should().Be(10);
    }

    [Test]
    public void DuplicatePositiveSources_AreWeightedButEveryTradeoffRemains()
    {
        var ship = Original();
        for (var i = 1; i <= 5; i++) ship.LowPowerModules[i] = new() {
            Design = "output_amplifier", ItemInstanceId = i.ToString() };
        ShipFittedStats.Recompute(ship, new Dictionary<SkillType, int> { [SkillType.Gunnery] = 50 });
        ShipFittedStats.Bonus(ship, StatType.ShipWeaponOutput).Should().BeApproximately(.25, 1e-9);
        ShipFittedStats.Bonus(ship, StatType.ShipWeaponCapacitorDemand).Should().BeApproximately(.50, 1e-9);
        // Increased demand is a downside, so it must never receive duplicate attenuation.
        ShipFittedStats.Penalty(ship, StatType.ShipWeaponCapacitorDemand).Should().Be(0);
    }

    [Test]
    public void RecomputingPools_KeepsDamageAndDoesNotStackPassives()
    {
        var ship = Original();
        ship.LowPowerModules[1] = new() { Design = "hull_plating", ItemInstanceId = "hull" };
        ship.LowPowerModules[2] = new() { Design = "armor_plating", ItemInstanceId = "armor" };
        ShipFittedStats.Recompute(ship);
        ship.MaxHull.Should().Be(200); ship.Hull.Should().Be(173);
        var baseResistance = ShipFittingCatalog.Default.Hulls[ship.ItemTag].Resistance;
        ship.HullResistance.Should().Be(baseResistance + 20); ship.ShieldResistance.Should().Be(0);
        var once = ship.Speed;
        ShipFittedStats.Recompute(ship);
        ship.Speed.Should().Be(once); ship.MaxHull.Should().Be(200);
        ship.LowPowerModules.Clear(); ShipFittedStats.Recompute(ship);
        ship.Hull.Should().Be(113);
        ship.ResourceDeficits.HullDamage.Should().Be(27);
    }

    [Test]
    public void InoperableModules_StillConsumePowerButGrantNoBonus()
    {
        var ship = Original();
        ship.LowPowerModules[1] = new() { Design = "hull_plating", Condition = 0 };
        ShipFittedStats.Recompute(ship);
        ship.FittingPowerUsed.Should().Be(12);
        ship.MaxHull.Should().Be(140);
        ship.Speed.Should().Be(ShipFittingCatalog.Default.Hulls[ship.ItemTag].Speed);
    }

    [Test]
    public void LegacyConversion_ExcludesEveryAmmunitionAndFuelBlueprint()
    {
        var catalog = ShipFittingCatalog.Default;
        catalog.LegacyModules.Should().HaveCount(143);
        foreach (var tag in new[] { "acm_ammo", "proton_bomb", "ship_fuelcapsule", "ship_missile" })
            catalog.LegacyModules.Should().NotContainKey(tag);
    }

    [Test]
    public void ShieldBank_UsesItsFlatCapacitorPenaltyAndCannotRefillThePool()
    {
        var ship = Original();
        ship.LowPowerModules[1] = new() { Design = "shield_bank" };
        ShipFittedStats.Recompute(ship);
        ship.MaxCapacitor.Should().Be(65);
        ship.Capacitor.Should().Be(36);
        ship.MaxShield.Should().Be(160);
        ship.Shield.Should().Be(128);
    }

    [Test]
    public void ResourceChanges_ReduceUnclampedDamageWithoutRefitRefills()
    {
        var ship = Original();
        ship.ResourceDeficits = new ShipResourceDeficits(160, 90, 90);
        ShipFittedStats.Recompute(ship);
        ship.Hull.Should().Be(1); ship.Capacitor.Should().Be(0);
        ShipResources.Restore(ship, ShipResource.Hull, 10).Should().Be(10);
        ship.ResourceDeficits.HullDamage.Should().Be(150);
        ship.Hull.Should().Be(1);
        ship.LowPowerModules[1] = new() { Design = "hull_plating" };
        ShipFittedStats.Recompute(ship);
        ship.Hull.Should().Be(50);
        ShipResources.Spend(ship, ShipResource.Hull, 7).Should().Be(7);
        ship.ResourceDeficits.HullDamage.Should().Be(157);
        ship.LowPowerModules.Clear(); ShipFittedStats.Recompute(ship);
        ship.Hull.Should().Be(1);
        ship.LowPowerModules[1] = new() { Design = "hull_plating" }; ShipFittedStats.Recompute(ship);
        ship.Hull.Should().Be(43);
    }

    [Test]
    public void ResourceRestoration_IsBoundedAndCannotCreateEnergy()
    {
        var ship = Original();
        ShipResources.Restore(ship, ShipResource.Capacitor, 1000).Should().Be(29);
        ship.Capacitor.Should().Be(40);
        ShipResources.Spend(ship, ShipResource.Capacitor, 41).Should().Be(40);
        ship.Capacitor.Should().Be(0);
        ShipResources.Restore(ship, ShipResource.Capacitor, 1).Should().Be(1);
        ship.Capacitor.Should().Be(1);
        var invalid = () => ShipResources.Spend(ship, ShipResource.Capacitor, -10);
        invalid.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void PerkAdjustments_UseDeclaredShipUnitsAndPositiveSpeedCap()
    {
        var ship = Original();
        ShipFittedStats.Recompute(ship, new Dictionary<SkillType, int> { [SkillType.Piloting] = 50 },
            new Dictionary<StatType, int> {
                [StatType.ShipSpeed] = 2000, [StatType.ShipCapacitorRecovery] = 60,
                [StatType.ShipHullResistance] = 100 });
        var hull = ShipFittingCatalog.Default.Hulls[ship.ItemTag];
        ship.Speed.Should().BeApproximately(hull.Speed * 1.25, 1e-9);
        ship.CapacitorRecovery.Should().BeApproximately(hull.CapacitorRecovery + .6, 1e-9);
        ship.HullResistance.Should().Be(60);
    }
}
