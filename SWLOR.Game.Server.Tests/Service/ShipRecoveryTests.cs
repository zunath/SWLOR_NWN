using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipRecoveryTests
{
    private static ShipStatus Ship() => ShipFittingConversion.Convert(new() { ItemTag = "ShipDeedLightFreighter", Hull = 50, MaxHull = 50, Shield = 50, MaxShield = 50, Capacitor = 40, MaxCapacitor = 40 });
    [Test]
    public void Defeat_RetainsEquipmentAndSettlesItsFlightOnlyOnce()
    {
        var ship = Ship(); ship.FlightId = "flight";
        ship.HighPowerModules.Add(1, new() { Design = "tracking_laser", ItemInstanceId = "laser", Condition = 100 });
        ship.ConfigurationModules.Add(1, new() { Design = "combat_conversion", ItemInstanceId = "configuration", Condition = 100 });
        ShipCargo.Add(ship, "ore_tilarium", 100, "cargo");
        ShipCargo.Add(ship, "light_missile", 50, "ammo");
        var result = ShipRecovery.Defeat(ship);
        result.Applied.Should().BeTrue(); result.LostCargo["ore_tilarium"].Should().Be(20);
        ShipCargo.Amount(ship, "light_missile").Should().Be(50);
        ship.HighPowerModules[1].ItemInstanceId.Should().Be("laser"); ship.HighPowerModules[1].Condition.Should().Be(80);
        ship.ConfigurationModules[1].Condition.Should().Be(80);
        ship = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(ship))!;
        ShipRecovery.Defeat(ship).Applied.Should().BeFalse();
        ship.OutstandingRecoveryCredits.Should().Be(result.RecoveryCredits); ship.HighPowerModules[1].Condition.Should().Be(80);
    }
    [Test]
    public void CargoLoss_RoundsPerCommodityAndProtectsCapacityBeforeLoss()
    {
        var ship = Ship(); ship.FlightId = "flight"; ship.ProtectedCargo = 10;
        ShipCargo.Add(ship, "ore_currian", 11, "one"); ShipCargo.Add(ship, "ore_currian", 9, "two");
        ShipCargo.Add(ship, "ore_tilarium", 4, "three"); ShipCargo.Add(ship, "ore_tilarium", 4, "four");
        var result = ShipRecovery.Defeat(ship);
        result.LostCargo["ore_currian"].Should().Be(2); result.LostCargo["ore_tilarium"].Should().Be(1);
    }
    [Test]
    public void PackedCargo_UsesProtectedOccupancyWithoutCreatingExtraCommodityLoss()
    {
        var ship = Ship(); ship.FlightId = "flight"; ship.ProtectedCargo = 10;
        ShipCargo.Add(ship, "ore_tilarium", 30, "packed", compressed: true);
        ShipRecovery.Defeat(ship).LostCargo["ore_tilarium"].Should().Be(2);
    }
    [Test]
    public void Service_RestoresZeroConditionAndDoesNotBillHullRecoveryTwice()
    {
        var ship = Ship(); ship.FlightId = "flight"; ship.Hull = 1;
        var module = new ShipStatus.ShipStatusModule { Design = "tracking_laser", Condition = 0, ItemInstanceId = "laser" };
        ship.HighPowerModules.Add(1, module);
        var hullPrice = ShipRecovery.HullRecoveryPrice(ship);
        ship.OutstandingRecoveryCredits = hullPrice;
        ShipRecovery.DockPrice(ship).Should().Be(hullPrice + ShipFittingCatalog.Default.Modules["tracking_laser"].ReferenceValue);
        ShipRecovery.CompleteDockService(ship);
        module.Condition.Should().Be(100); ship.Hull.Should().Be(ship.MaxHull);
        ship.OutstandingRecoveryCredits.Should().Be(0); ShipRecovery.DockPrice(ship).Should().Be(0);
    }
    [Test]
    public void ConditionAboveZero_PreservesModuleOutputAndUsesTheDeclaredServiceRate()
    {
        var ship = Ship(); var module = new ShipStatus.ShipStatusModule { Design = "tracking_laser", Condition = 100 };
        var output = ShipOperations.Resolve(ship, module).Output;
        module.Condition = 1;
        ShipOperations.Resolve(ship, module).Output.Should().Be(output);
        ShipRecovery.ModuleServicePrice(module).Should().Be((int)Math.Ceiling(ShipFittingCatalog.Default.Modules[module.Design].ReferenceValue * .99));
        module.Condition = 0;
        var disabled = () => ShipOperations.Resolve(ship, module);
        disabled.Should().Throw<InvalidOperationException>();
    }
}
