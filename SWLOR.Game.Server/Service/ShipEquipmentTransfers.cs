using System;
using System.Linq;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service
{
    public static class ShipEquipmentTransfers
    {
        public static IEnumerable<PlayerShip> AllShips()
        {
            for(var offset=0;;offset+=200)
            {
                var page=DB.Search(new DBQuery<PlayerShip>().OrderBy(nameof(PlayerShip.Id)).AddPaging(200,offset)).ToArray();
                foreach(var ship in page)yield return ship;
                if(page.Length<200)yield break;
            }
        }
        public static PlayerShip RequireDock(uint player, string shipId)
        {
            if (!GetIsPC(player) || Space.IsPlayerInSpaceMode(player)) throw new InvalidOperationException("Dock before refitting or withdrawing equipment.");
            var ship = DB.Get<PlayerShip>(shipId) ?? throw new InvalidOperationException("Ship unavailable.");
            var property = DB.Get<WorldProperty>(ship.PropertyId) ?? throw new InvalidOperationException("Ship dock unavailable.");
            if (property.Positions.ContainsKey(PropertyLocationType.CurrentPosition) || !property.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dock))
                throw new InvalidOperationException("Dock before refitting or withdrawing equipment.");
            uint area;
            if (!string.IsNullOrWhiteSpace(dock.AreaResref)) area = Area.GetAreaByResref(dock.AreaResref);
            else area = Property.TryGetLoadedInstance(dock.InstancePropertyId, out var instance) ? instance.Area : OBJECT_INVALID;
            if (!GetIsObjectValid(area) || area != GetArea(player)) throw new InvalidOperationException("Visit this ship's dock to manage its equipment.");
            var playerId = GetObjectUUID(player);
            var permissions = DB.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), ship.PropertyId, false));
            if (property.OwnerPlayerId != playerId && !permissions.Any(x => x.Permissions.GetValueOrDefault(PropertyPermissionType.RefitShip)))
                throw new InvalidOperationException("You do not have permission to refit this ship.");
            EnsureFitting(ship);
            return ship;
        }

        public static void EnsureFitting(PlayerShip ship)
        {
            if (ship.Status.FittingVersion >= ShipFittingConversion.CurrentVersion) return;
            ship.Status = ShipFittingConversion.Convert(ship.Status, sourceIdentity: ship.Id);
            DB.Set(ship);
        }

        public static void Install(uint player, string shipId, ShipFittingBank bank, int slot, uint item)
        {
            var ship = RequireDock(player, shipId);
            if (GetItemPossessor(item) != player) throw new InvalidOperationException("Choose equipment in your inventory.");
            if (ship.Status.PendingInventoryTransfers.Count != 0) throw new InvalidOperationException("Complete the pending equipment transfer first.");
            if(!Item.CanCreatureUseItem(player,item))throw new InvalidOperationException("This bound equipment belongs to another character.");
            var equipment = ShipEquipment.Read(item);
            if(!string.IsNullOrEmpty(equipment.BoundPlayerId)&&ship.OwnerPlayerId!=equipment.BoundPlayerId)throw new InvalidOperationException("Fit starter equipment only to your own ship.");
            if (ShipEquipmentTransfers.AllShips().Any(other =>
                ShipFittedStats.Modules(other.Status).Concat(other.Status.ConfigurationModules.Values).Any(x => x.ItemInstanceId == equipment.ItemInstanceId) ||
                other.Status.RefitRecovery.ContainsKey(equipment.ItemInstanceId) || other.Status.PendingInventoryTransfers.ContainsKey(equipment.ItemInstanceId)))
                throw new InvalidOperationException("This equipment already belongs to a ship or pending transfer.");
            var fitted = ShipRefitting.Equip(ship.Status, bank, slot, equipment,
                Space.GetOperatingSkills(player), DateTime.UtcNow, Space.GetShipStatAdjustments(player));
            // Persist the inventory identity before the ship can claim ownership of this item.
            ExportSingleCharacter(player);
            if (equipment.OriginalSerializedItem != null) fitted.LegacyEquipmentAudit[equipment.ItemInstanceId] = equipment.OriginalSerializedItem;
            ShipEquipmentOwnership.Begin(fitted, equipment.ItemInstanceId, GetObjectUUID(player), ShipInventoryTransferDirection.Install);
            ship.Status = fitted;
            DB.Set(ship);
            RemoveInventorySourceThenSettle(player, shipId, equipment.ItemInstanceId, GetObjectUUID(player));
        }

        public static string Remove(uint player, string shipId, ShipFittingBank bank, int slot)
        {
            var ship = RequireDock(player, shipId);
            if (ship.Status.PendingInventoryTransfers.Count != 0) throw new InvalidOperationException("Complete the pending equipment transfer first.");
            var id = ShipRefitting.Bank(ship.Status, bank).TryGetValue(slot, out var module) ? module.ItemInstanceId : null;
            if (id == null) throw new InvalidOperationException("No equipment is installed in that slot.");
            ship.Status = ShipRefitting.Remove(ship.Status, bank, slot, DateTime.UtcNow,
                Space.GetOperatingSkills(player), Space.GetShipStatAdjustments(player));
            DB.Set(ship);
            Withdraw(player, shipId, id);
            return id;
        }

        public static void Withdraw(uint player, string shipId, string equipmentId)
        {
            var ship = RequireDock(player, shipId);
            if (ship.Status.PendingInventoryTransfers.Count != 0) throw new InvalidOperationException("Complete the pending equipment transfer first.");
            if (!ship.Status.RefitRecovery.ContainsKey(equipmentId)) throw new InvalidOperationException("No recoverable equipment with that identity.");
            ShipEquipmentOwnership.Begin(ship.Status, equipmentId, GetObjectUUID(player), ShipInventoryTransferDirection.Withdraw);
            DB.Set(ship);
            CompleteWithdrawal(player, shipId, equipmentId);
        }

        private static uint FindInventoryItem(uint player, string identity)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
                if (GetLocalString(item, ShipEquipment.IdentityVariable) == identity) return item;
            return OBJECT_INVALID;
        }

        private static void RemoveInventorySourceThenSettle(uint player, string shipId, string identity, string playerId)
        {
            var item = FindInventoryItem(player, identity);
            if (GetIsObjectValid(item)) DestroyObject(item);
            // Native destruction can be deferred until the current script finishes.
            // Keep the durable lease until the inventory no longer contains the source.
            DelayCommand(.1f, () =>
            {
                if (!GetIsObjectValid(player) || GetObjectUUID(player) != playerId || GetIsObjectValid(FindInventoryItem(player, identity))) return;
                ExportSingleCharacter(player);
                FinishInstall(shipId, identity, playerId);
            });
        }

        private static void FinishInstall(string shipId, string identity, string playerId)
        {
            var ship = DB.Get<PlayerShip>(shipId);
            ShipEquipmentOwnership.Settle(ship.Status, identity, playerId, ShipInventoryTransferDirection.Install, false);
            DB.Set(ship);
        }

        private static void CompleteWithdrawal(uint player, string shipId, string identity)
        {
            var ship = DB.Get<PlayerShip>(shipId);
            if (!GetIsObjectValid(FindInventoryItem(player, identity)))
                ShipEquipment.CreateForWithdrawal(ship.Status.RefitRecovery[identity], player);
            ExportSingleCharacter(player);
            // The saved character now owns the item. Remove the ship's recovery entitlement.
            ship = DB.Get<PlayerShip>(shipId);
            ShipEquipmentOwnership.Settle(ship.Status, identity, GetObjectUUID(player), ShipInventoryTransferDirection.Withdraw, true);
            DB.Set(ship);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverPendingInventoryTransfers()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            var playerId = GetObjectUUID(player);
            // Include delegated refits: the transferring player can differ from the ship owner.
            foreach (var ship in ShipEquipmentTransfers.AllShips().ToArray())
            foreach (var (identity, transfer) in ship.Status.PendingInventoryTransfers.Where(x => x.Value.PlayerId == playerId).ToArray())
            {
                if (transfer.Direction == ShipInventoryTransferDirection.Install)
                {
                    RemoveInventorySourceThenSettle(player, ship.Id, identity, playerId);
                }
                else CompleteWithdrawal(player, ship.Id, identity);
            }
        }
    }
}
