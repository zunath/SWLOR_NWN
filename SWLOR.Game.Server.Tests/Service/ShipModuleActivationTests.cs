using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipModuleActivationTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
    private static ShipStatus Ship() => new() { FittingVersion = 1, Hull = 100, MaxHull = 100, Capacitor = 50, MaxCapacitor = 50, CargoCapacity = 100 };
    private static ShipStatus.ShipStatusModule Module(string design) => new() { Design = design, ItemInstanceId = "tool" };
    private static ShipActivationContext Context() => new(true, true, true, true, false, false, false, 20, 50);

    [Test]
    public void BothOperators_UseRangeSkillAndConditionChecksWithoutBypasses()
    {
        var ship = Ship(); var fitted = Module("heavy_beam"); var operation = ShipOperations.Resolve(ship, fitted);
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context(), Now).Should().BeNull();
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context() with { OperatorRank = 0 }, Now).Should().Contain("Requires");
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context() with { Distance = 100 }, Now).Should().Contain("outside");
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context() with { SameArea = false }, Now).Should().Contain("area");
        fitted.Condition = 0;
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context(), Now).Should().Contain("servicing");
    }

    [Test]
    public void PaidActivation_ConsumesOneFiniteMissileAndUsesOneSecondCadence()
    {
        var ship = Ship(); var fitted = Module("rapid_missile"); var operation = ShipOperations.Resolve(ship, fitted);
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context(), Now).Should().Contain("Load");
        ShipCargo.Add(ship, "light_missile", 2, "load");
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context(), Now).Should().BeNull();
        ShipModuleActivationPolicy.Pay(ship, fitted, operation, Now);
        ShipCargo.Amount(ship, "light_missile").Should().Be(1);
        ship.Capacitor.Should().Be(50 - operation.CapacitorCost);
        ship.GlobalRecast.Should().Be(Now.AddSeconds(1));
        fitted.RecastTime.Should().Be(Now.AddSeconds(operation.Variant.Cycle));
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context(), Now.AddSeconds(1)).Should().Contain("not ready");
    }

    [Test]
    public void Transfers_RejectSelfAndKeepThePaidEnergyIndependentOfOutputBonuses()
    {
        var ship = Ship(); ship.FittingBonuses[StatType.ShipCapacitorDiscount] = .9; ship.FittingBonuses[StatType.ShipRecoveryOutput] = .4;
        var fitted = Module("transfer_projector"); var operation = ShipOperations.Resolve(ship, fitted);
        operation.CapacitorCost.Should().Be(20); operation.Output.Should().Be(20);
        var allied = Context() with { Hostile = false, Allied = true };
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, allied, Now).Should().BeNull();
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, allied with { SelfTarget = true }, Now).Should().Contain("another");
    }

    [Test]
    public void DefeatPendingRefitAndActivationControl_BlockPayment()
    {
        var ship = Ship(); var fitted = Module("tracking_laser"); var operation = ShipOperations.Resolve(ship, fitted);
        ship.Hull = 0; ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context(), Now).Should().Contain("operational");
        ship.Hull = 100; ship.RefitReadyAt = Now.AddSeconds(5);
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context(), Now).Should().Contain("refit");
        ship.RefitReadyAt = default;
        ShipModuleActivationPolicy.Validate(ship, fitted, operation, Context(), Now,
            new Dictionary<StatType, double> { [StatType.ShipActivationLock] = 1 }).Should().Contain("cannot activate");
    }
}
