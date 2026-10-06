using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipEquipmentOwnershipTests
{
    private static ShipStatus Copy(ShipStatus status) => JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status))!;

    [Test]
    public void InterruptedInstall_ResumesWithoutCreatingASecondItemOrResettingItsCooldown()
    {
        var module = new ShipStatus.ShipStatusModule { ItemInstanceId = "equipment", RecastTime = DateTime.UtcNow.AddHours(1), Condition = 80 };
        var ship = new ShipStatus(); ship.HighPowerModules.Add(1, module);
        ShipEquipmentOwnership.Begin(ship, module.ItemInstanceId, "delegate", ShipInventoryTransferDirection.Install);
        var inventory = new HashSet<string> { module.ItemInstanceId };
        var resumed = Copy(ship);
        inventory.Remove(module.ItemInstanceId);
        ShipEquipmentOwnership.Settle(resumed, module.ItemInstanceId, "delegate", ShipInventoryTransferDirection.Install, inventory.Contains(module.ItemInstanceId)).Should().BeTrue();
        ShipEquipmentOwnership.Settle(resumed, module.ItemInstanceId, "delegate", ShipInventoryTransferDirection.Install, false).Should().BeFalse();
        resumed.HighPowerModules[1].RecastTime.Should().Be(module.RecastTime);
        resumed.HighPowerModules[1].Condition.Should().Be(80);
        resumed.PendingInventoryTransfers.Should().BeEmpty(); inventory.Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InterruptedWithdrawal_ReplaysUntilInventoryOwnsExactlyOneItem(bool savedBeforeInterruption)
    {
        var ship = new ShipStatus();
        ship.RefitRecovery.Add("equipment", new() { ItemInstanceId = "equipment", OriginalSerializedItem = "audit" });
        ship.RefitRecoveryReasons.Add("equipment", "Displaced item retained.");
        ship.LegacyEquipmentAudit.Add("equipment", "audit");
        ShipEquipmentOwnership.Begin(ship, "equipment", "delegate", ShipInventoryTransferDirection.Withdraw);
        ship = Copy(ship);
        var inventory = savedBeforeInterruption ? new HashSet<string> { "equipment" } : new HashSet<string>();
        inventory.Add("equipment");
        ShipEquipmentOwnership.Settle(ship, "equipment", "delegate", ShipInventoryTransferDirection.Withdraw, true).Should().BeTrue();
        ShipEquipmentOwnership.Settle(ship, "equipment", "delegate", ShipInventoryTransferDirection.Withdraw, true).Should().BeFalse();
        inventory.Should().ContainSingle(); ship.RefitRecovery.Should().BeEmpty();
        ship.PendingInventoryTransfers.Should().BeEmpty(); ship.RefitRecoveryReasons.Should().BeEmpty();
        ship.LegacyEquipmentAudit["equipment"].Should().Be("audit");
    }

    [Test]
    public void PrematureSettlementAndOtherPlayers_CannotRemoveRecoveryOwnership()
    {
        var ship = new ShipStatus(); ship.RefitRecovery.Add("equipment", new() { ItemInstanceId = "equipment" });
        ShipEquipmentOwnership.Begin(ship, "equipment", "delegate", ShipInventoryTransferDirection.Withdraw);
        Action premature = () => ShipEquipmentOwnership.Settle(ship, "equipment", "delegate", ShipInventoryTransferDirection.Withdraw, false);
        Action unauthorized = () => ShipEquipmentOwnership.Settle(ship, "equipment", "owner", ShipInventoryTransferDirection.Withdraw, true);
        Action second = () => ShipEquipmentOwnership.Begin(ship, "other", "delegate", ShipInventoryTransferDirection.Withdraw);
        premature.Should().Throw<InvalidOperationException>(); unauthorized.Should().Throw<InvalidOperationException>(); second.Should().Throw<InvalidOperationException>();
        ship.RefitRecovery.Should().ContainKey("equipment"); ship.PendingInventoryTransfers.Should().ContainKey("equipment");
    }
}
