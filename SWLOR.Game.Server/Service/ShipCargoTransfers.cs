using System;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class ShipCargoTransfers
    {
        private const string TransferVariable = "SHIP_CARGO_TRANSFER";
        public static bool IsCargoResource(string resref) => SpaceIndustryCatalog.Default.Commodities.ContainsKey(resref) ||
            SpaceIndustryCatalog.Default.Commodities.Values.Any(x => x.Metal == resref) ||
            ShipFittingCatalog.Default.Modules.Values.Any(x => ShipModuleActivationPolicy.Supply(x) == ShipCargo.SupplyIdentity(resref)) ||
            resref is "elec_recover" or "elec_ruined";

        public static void Load(uint player, string shipId, uint item)
        {
            var ship = ShipEquipmentTransfers.RequireDock(player, shipId);
            if (GetItemPossessor(item) != player || GetPlotFlag(item) || Item.IsEconomyRestricted(item) || !IsCargoResource(GetResRef(item)))
                throw new InvalidOperationException("Select ore, metal, salvage material, or ship supplies in your inventory.");
            var identity = GetLocalString(item, TransferVariable);
            if (string.IsNullOrEmpty(identity))
            { identity = Guid.NewGuid().ToString(); SetLocalString(item, TransferVariable, identity); }
            if (DB.Search(new DBQuery<PlayerShip>()).Any(x => x.Status.PendingCargoTransfers.Values.Any(t => t.InventoryId == identity)))
                throw new InvalidOperationException("This stack already has a pending cargo transfer.");
            var transfer = new ShipCargoTransfer(Guid.NewGuid().ToString(), GetObjectUUID(player), ShipCargoTransferDirection.Load,
                GetResRef(item), GetItemStackSize(item), identity);
            ShipCargoOwnership.BeginLoad(ship.Status, transfer);
            ExportSingleCharacter(player);
            DB.Set(ship);
            CompleteLoad(player, shipId, transfer);
        }

        public static void Withdraw(uint player, string shipId, string lotId)
        {
            var ship = ShipEquipmentTransfers.RequireDock(player, shipId);
            if (!ship.Status.Cargo.TryGetValue(lotId, out var lot)) throw new InvalidOperationException("That cargo is no longer in the hold.");
            var transfer = new ShipCargoTransfer(Guid.NewGuid().ToString(), GetObjectUUID(player), ShipCargoTransferDirection.Withdraw,
                lot.Resref, Math.Min(20, (int)Math.Floor(lot.Quantity + 1e-9)), Guid.NewGuid().ToString(), lotId);
            if (transfer.Quantity == 0) throw new InvalidOperationException("Fractional recovery remains in the hold until it forms a whole item.");
            ShipCargoOwnership.BeginWithdrawal(ship.Status, transfer); DB.Set(ship);
            CompleteWithdrawal(player, shipId, transfer);
        }

        private static uint InventoryItem(uint player, string id)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
                if (GetLocalString(item, TransferVariable) == id) return item;
            return OBJECT_INVALID;
        }
        private static void CompleteLoad(uint player, string shipId, ShipCargoTransfer transfer)
        {
            var item = InventoryItem(player, transfer.InventoryId);
            if (GetIsObjectValid(item)) DestroyObject(item);
            DelayCommand(.1f, () =>
            {
                if (!GetIsObjectValid(player) || GetObjectUUID(player) != transfer.PlayerId || GetIsObjectValid(InventoryItem(player, transfer.InventoryId))) return;
                ExportSingleCharacter(player);
                var ship = DB.Get<PlayerShip>(shipId);
                ShipCargoOwnership.Settle(ship.Status, transfer.Id, transfer.PlayerId, false); DB.Set(ship);
            });
        }
        private static void CompleteWithdrawal(uint player, string shipId, ShipCargoTransfer transfer)
        {
            var item = InventoryItem(player, transfer.InventoryId);
            if (!GetIsObjectValid(item))
            {
                item = CreateItemOnObject(transfer.Resref, player, transfer.Quantity);
                if (!GetIsObjectValid(item) || GetItemPossessor(item) != player)
                {
                    if (GetIsObjectValid(item)) DestroyObject(item);
                    throw new InvalidOperationException("Make inventory room, then retry the pending cargo transfer.");
                }
                SetLocalString(item, TransferVariable, transfer.InventoryId);
                if (GetItemStackSize(item) != transfer.Quantity)
                { DestroyObject(item); throw new InvalidOperationException("Cargo delivery did not create the expected stack. The transfer remains recoverable."); }
            }
            if (GetItemStackSize(item) != transfer.Quantity)
                throw new InvalidOperationException("The pending delivery stack changed. Contact staff to reconcile its recorded quantity.");
            ExportSingleCharacter(player);
            var ship = DB.Get<PlayerShip>(shipId);
            ShipCargoOwnership.Settle(ship.Status, transfer.Id, transfer.PlayerId, true); DB.Set(ship);
        }

        public static void Recover(uint player, string shipId)
        {
            var ship = DB.Get<PlayerShip>(shipId);
            foreach (var transfer in ship.Status.PendingCargoTransfers.Values.Where(x => x.PlayerId == GetObjectUUID(player)).ToArray())
            {
                if (transfer.Direction == ShipCargoTransferDirection.Load) CompleteLoad(player, shipId, transfer);
                else CompleteWithdrawal(player, shipId, transfer);
            }
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverCargoOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            foreach (var ship in DB.Search(new DBQuery<PlayerShip>()).Where(x => x.Status.PendingCargoTransfers.Values.Any(t => t.PlayerId == GetObjectUUID(player))).ToArray())
            {
                try { Recover(player, ship.Id); }
                catch (InvalidOperationException ex) { SendMessageToPC(player, ex.Message); }
            }
        }
    }
}
