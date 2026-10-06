using System;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipEquipmentOwnership
    {
        public static void Begin(ShipStatus status, string identity, string playerId, ShipInventoryTransferDirection direction)
        {
            if (string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(playerId) || !Enum.IsDefined(direction))
                throw new ArgumentException("Equipment and receiving-player identities are required.");
            if (status.PendingInventoryTransfers.Count != 0) throw new InvalidOperationException("Complete the pending equipment transfer first.");
            var installed = ShipFittedStats.Modules(status).Concat(status.ConfigurationModules.Values).Any(x => x.ItemInstanceId == identity);
            var recoverable = status.RefitRecovery.ContainsKey(identity);
            if (direction == ShipInventoryTransferDirection.Install ? !installed || recoverable : !recoverable || installed)
                throw new InvalidOperationException("Equipment ownership does not match the requested transfer.");
            status.PendingInventoryTransfers.Add(identity, new(playerId, direction));
        }

        public static bool Settle(ShipStatus status, string identity, string playerId, ShipInventoryTransferDirection direction,
            bool savedInventoryContainsItem)
        {
            if (!status.PendingInventoryTransfers.TryGetValue(identity, out var pending)) return false;
            if (pending.PlayerId != playerId || pending.Direction != direction)
                throw new InvalidOperationException("Only the recorded transferring player may settle this item.");
            if (savedInventoryContainsItem != (direction == ShipInventoryTransferDirection.Withdraw))
                throw new InvalidOperationException("Save the inventory transfer before settling ship ownership.");
            if (direction == ShipInventoryTransferDirection.Withdraw)
            {
                status.RefitRecovery.Remove(identity);
                status.RefitRecoveryReasons.Remove(identity);
            }
            status.PendingInventoryTransfers.Remove(identity);
            return true;
        }
    }
}
