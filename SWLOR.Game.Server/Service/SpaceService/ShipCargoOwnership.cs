using System;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public enum ShipCargoTransferDirection { Load, Withdraw }
    public sealed record ShipCargoTransfer(string Id, string PlayerId, ShipCargoTransferDirection Direction,
        string Resref, int Quantity, string InventoryId, string LotId = null);

    public static class ShipCargoOwnership
    {
        public static void BeginLoad(ShipStatus status, ShipCargoTransfer transfer)
        {
            Validate(status, transfer, ShipCargoTransferDirection.Load);
            if (!ShipCargo.CanAdd(status, transfer.Quantity)) throw new InvalidOperationException("The ship's hold does not have room for that stack.");
            status.PendingCargoTransfers.Add(transfer.Id, transfer);
        }
        public static void BeginWithdrawal(ShipStatus status, ShipCargoTransfer transfer)
        {
            Validate(status, transfer, ShipCargoTransferDirection.Withdraw);
            if (!status.Cargo.TryGetValue(transfer.LotId, out var lot) || lot.ContractId != null || lot.Resref != transfer.Resref || transfer.Quantity > Math.Floor(lot.Quantity + 1e-9))
                throw new InvalidOperationException("That cargo is unavailable or belongs to a freight employer.");
            if (Math.Abs(lot.Quantity - transfer.Quantity) <= 1e-9) status.Cargo.Remove(transfer.LotId);
            else status.Cargo[transfer.LotId] = lot with { Quantity = lot.Quantity - transfer.Quantity };
            status.PendingCargoTransfers.Add(transfer.Id, transfer);
        }
        private static void Validate(ShipStatus status, ShipCargoTransfer transfer, ShipCargoTransferDirection direction)
        {
            if (transfer == null || string.IsNullOrEmpty(transfer.Id) || string.IsNullOrEmpty(transfer.PlayerId) || string.IsNullOrEmpty(transfer.InventoryId) ||
                string.IsNullOrEmpty(transfer.Resref) || transfer.Quantity <= 0 || transfer.Direction != direction)
                throw new ArgumentException("Expected a complete cargo ownership transfer.");
            if (status.PendingCargoTransfers.Count > 0 || status.PendingInventoryTransfers.Count > 0) throw new InvalidOperationException("Finish the ship's pending transfer first.");
        }
        public static bool Settle(ShipStatus status, string id, string actor, bool savedInventoryContainsItem)
        {
            if (!status.PendingCargoTransfers.TryGetValue(id, out var transfer)) return false;
            if (transfer.PlayerId != actor) throw new InvalidOperationException("Only the recorded cargo actor can settle this transfer.");
            if (savedInventoryContainsItem != (transfer.Direction == ShipCargoTransferDirection.Withdraw)) throw new InvalidOperationException("Save the final inventory ownership before settling cargo.");
            status.PendingCargoTransfers.Remove(id);
            if (transfer.Direction == ShipCargoTransferDirection.Load)
                status.Cargo.Add(transfer.Id, new ShipCargoLot(transfer.Resref, transfer.Quantity));
            return true;
        }
    }
}
