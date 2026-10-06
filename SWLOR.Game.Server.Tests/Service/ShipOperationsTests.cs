using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipOperationsTests
{
    private static ShipStatus Status() => new() { MaxHull = 100, Hull = 100, MaxShield = 10, Shield = 10, MaxCapacitor = 50, Capacitor = 0 };

    [Test]
    public void FractionalDamageAndRegeneration_AreConservedAcrossSerialization()
    {
        var status = Status();
        for (var i = 0; i < 4; i++) ShipResources.SpendPrecise(status, ShipResource.Hull, .25);
        status.Hull.Should().Be(99);
        status = Newtonsoft.Json.JsonConvert.DeserializeObject<ShipStatus>(Newtonsoft.Json.JsonConvert.SerializeObject(status))!;
        for (var i = 0; i < 3; i++) ShipResources.RestorePrecise(status, ShipResource.Hull, .25);
        ShipResources.Available(status, ShipResource.Hull).Should().BeApproximately(99.75, 1e-9);
        ShipResources.RestorePrecise(status, ShipResource.Hull, .25);
        status.Hull.Should().Be(100);
        status.ResourceDeficits!.HullDamage.Should().Be(0);
        status.FractionalResourceDeficits[ShipResource.Hull].Should().Be(0);
    }

    [Test]
    public void ShieldOverflow_UsesEachPoolsResistanceAndWeaponMultiplier()
    {
        var status = Status(); status.ShieldResistance = 60; status.HullResistance = 0;
        var damage = ShipOperations.ApplyDamage(status, 30, 1.3, .5);
        damage.Shield.Should().BeApproximately(10, 1e-9);
        damage.Hull.Should().BeApproximately((30 - 10 / (1.3 / 1.6)) * .5, 1e-9);
        status.Shield.Should().Be(0);
        ShipOperations.ApplyDamage(status, 10000);
        status.Hull.Should().Be(0);
    }

    [Test]
    public void Recovery_UsesTheTenSecondHostileGateAndKeepsFractionalCapacitor()
    {
        var status = Status(); status.Shield = 0; status.CapacitorRecovery = .6; status.OutOfCombatShieldRecovery = .5;
        var now = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        status.LastHostileActivity = now;
        for (var i = 1; i <= 9; i++) ShipOperations.Recover(status, now.AddSeconds(i));
        status.Shield.Should().Be(0);
        ShipResources.Available(status, ShipResource.Capacitor).Should().BeApproximately(5.4, 1e-9);
        ShipOperations.Recover(status, now.AddSeconds(10));
        ShipResources.Available(status, ShipResource.Shield).Should().BeApproximately(.5, 1e-9);
        ShipResources.Available(status, ShipResource.Capacitor).Should().BeApproximately(6, 1e-9);
    }

    [Test]
    public void RefinementAndPerks_ShareThePermanentOutputCapWithoutErasingCalibrationDrawbacks()
    {
        var status = Status(); status.FittingBonuses[StatType.ShipWeaponOutput] = .4;
        var fitted = new ShipStatus.ShipStatusModule { Design = "tracking_laser", Calibration = "Efficient", QualityDimension = ShipQualityDimension.Output, Quality = 100 };
        var operation = ShipOperations.Resolve(status, fitted, 26);
        var catalog = ShipFittingCatalog.Default;
        var profile = catalog.Modules[fitted.Design]; var baseline = catalog.GetVariant(fitted.Design, fitted.Calibration);
        operation.Output.Should().BeApproximately(profile.Output * (1.4 - Math.Max(0, 1 - baseline.Output / profile.Output)), 1e-9);
        ShipOperations.Resolve(status, fitted, 26, new Dictionary<StatType, double> { [StatType.ShipWeaponOutput] = 10 }).Output
            .Should().BeApproximately(operation.Output + profile.Output * .3, 1e-9);
    }

    [Test]
    public void DisabledModulesAndInfiniteSources_AreRejected()
    {
        var status = Status();
        Action resolve = () => ShipOperations.Resolve(status, new() { Design = "tracking_laser", Condition = 0 });
        resolve.Should().Throw<InvalidOperationException>();
        Action damage = () => ShipOperations.ApplyDamage(status, double.PositiveInfinity);
        damage.Should().Throw<ArgumentException>();
        ShipOperations.ResistanceMultiplier(1000, new[] { 10d, 1000d }).Should().BeApproximately(100d / 185, 1e-9);
    }

    [Test]
    public void InjectorOutput_DoesNotScaleWithRecoveryMastery()
    {
        var status = Status(); status.FittingBonuses[StatType.ShipRecoveryOutput] = .4;
        ShipOperations.Resolve(status, new() { Design = "fuel_injector" }).Output.Should().Be(30);
    }
    [Test]
    public void TemporaryOutputAndSpeedCaps_KeepAllDeclaredDrawbacksAfterCappingPositiveSources()
    {
        var status = Status(); status.Speed = 1; status.BaseSpeed = 1;
        var now = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
        ShipTemporaryStats.Add(status, StatType.ShipWeaponOutput, .5, 10, "burst", now);
        ShipTemporaryStats.Add(status, StatType.ShipWeaponOutput, -.15, 10, "evasive", now);
        ShipTemporaryStats.Add(status, StatType.ShipSpeed, .5, 10, "escape", now);
        ShipTemporaryStats.Add(status, StatType.ShipSpeed, -.1, 10, "engine", now);
        var operation = ShipOperations.Resolve(status, new() { Design = "tracking_laser" }, temporarySources: ShipTemporaryStats.Sources(status, now));
        operation.Output.Should().BeApproximately(12 * 1.15, 1e-9);
        ShipOperations.MovementSpeed(status, now).Should().BeApproximately(1.25, 1e-9);
    }

}
