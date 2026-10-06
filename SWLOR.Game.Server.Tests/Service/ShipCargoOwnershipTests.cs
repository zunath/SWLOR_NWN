using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipCargoOwnershipTests
{
    [Test]
    public void LoadedCargo_StaysReservedUntilSourceInventoryRemovalIsSaved()
    {
        var ship = new ShipStatus { CargoCapacity = 100 };
        var transfer = new ShipCargoTransfer("load", "actor", ShipCargoTransferDirection.Load, "ore_tilarium", 40, "inventory");
        ShipCargoOwnership.BeginLoad(ship, transfer);
        ShipCargo.Amount(ship, "ore_tilarium").Should().Be(0); ShipCargo.Available(ship).Should().Be(60);
        var early = () => ShipCargoOwnership.Settle(ship, "load", "actor", true);
        early.Should().Throw<InvalidOperationException>();
        ship = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(ship))!;
        ShipCargoOwnership.Settle(ship, "load", "actor", false).Should().BeTrue();
        ShipCargoOwnership.Settle(ship, "load", "actor", false).Should().BeFalse();
        ShipCargo.Amount(ship, "ore_tilarium").Should().Be(40); ShipCargo.Available(ship).Should().Be(60);
    }
    [Test]
    public void Withdrawal_ReservesWholeItemsAndPreservesFractionalRecoveryAcrossRestart()
    {
        var ship = new ShipStatus { CargoCapacity = 100 };
        ShipCargo.Add(ship, "ore_currian", 12.6, "claim");
        var transfer = new ShipCargoTransfer("withdraw", "actor", ShipCargoTransferDirection.Withdraw, "ore_currian", 12, "inventory", "claim");
        ShipCargoOwnership.BeginWithdrawal(ship, transfer);
        ShipCargo.Amount(ship, "ore_currian").Should().BeApproximately(.6, 1e-9);
        ship = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(ship))!;
        var unowned = () => ShipCargoOwnership.Settle(ship, "withdraw", "other", true);
        unowned.Should().Throw<InvalidOperationException>();
        ShipCargoOwnership.Settle(ship, "withdraw", "actor", true).Should().BeTrue();
        ShipCargoOwnership.Settle(ship, "withdraw", "actor", true).Should().BeFalse();
        ShipCargo.Amount(ship, "ore_currian").Should().BeApproximately(.6, 1e-9);
    }
    [Test]
    public void EmployerCargo_IsExcludedFromPlayerSuppliesAndWithdrawals()
    {
        var ship = new ShipStatus { CargoCapacity = 100 };
        ShipCargo.Add(ship, "light_missile", 30, "freight", contractId: "contract");
        ShipCargo.Amount(ship, "light_missile").Should().Be(0);
        var consume = () => ShipCargo.Consume(ship, "light_missile", 1);
        consume.Should().Throw<InvalidOperationException>();
        var withdraw = () => ShipCargoOwnership.BeginWithdrawal(ship, new("withdraw", "actor", ShipCargoTransferDirection.Withdraw, "light_missile", 10, "inventory", "freight"));
        withdraw.Should().Throw<InvalidOperationException>();
        ship.Cargo["freight"].Quantity.Should().Be(30);
    }
    [Test]
    public void LegacySupplies_RetainPhysicalIdentityAndQuantityWhileServingCurrentModules()
    {
        var ship = new ShipStatus { CargoCapacity = 100 };
        ShipCargo.Add(ship, "ship_missile", 40, "legacy");
        ShipCargo.Amount(ship, "light_missile").Should().Be(40);
        ShipCargo.Consume(ship, "light_missile", 1);
        ship.Cargo["legacy"].Resref.Should().Be("ship_missile"); ship.Cargo["legacy"].Quantity.Should().Be(39);
    }
    [Test]
    public void OverflowCargo_CanBeWithdrawnButCannotAcceptAnotherLoad()
    {
        var ship = new ShipStatus { CargoCapacity = 20, Cargo = new() { ["legacy"] = new("ore_tilarium", 100) } };
        ShipCargo.Available(ship).Should().Be(0);
        var load = () => ShipCargoOwnership.BeginLoad(ship, new("load", "actor", ShipCargoTransferDirection.Load, "ore_currian", 1, "inventory"));
        load.Should().Throw<InvalidOperationException>();
        ShipCargoOwnership.BeginWithdrawal(ship, new("withdraw", "actor", ShipCargoTransferDirection.Withdraw, "ore_tilarium", 20, "inventory", "legacy"));
        ShipCargoOwnership.Settle(ship, "withdraw", "actor", true);
        ship.Cargo["legacy"].Quantity.Should().Be(80);
    }
}
