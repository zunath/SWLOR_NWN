using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Ship modules and deeds moving between a character's inventory and a ship record (fitting, removing, registering,
    /// unregistering). The item is marked and saved with the character before the ship record changes, so a restart in
    /// between leaves a marked item the next login settles from the ship as saved: an item the ship took goes, and a
    /// ship change that never happened is undone or finished.
    /// </summary>
    public static partial class Space
    {
        /// <summary>On a module being fitted: "ship id|module instance id".</summary>
        private const string ModuleFittingVariable = "SHIP_MODULE_FITTING";

        /// <summary>On a module taken out of a ship: "ship id|module instance id".</summary>
        private const string ModuleRemovedVariable = "SHIP_MODULE_REMOVED";

        /// <summary>On a deed being registered as a ship: the ship id.</summary>
        private const string DeedRegisteringVariable = "SHIP_DEED_REGISTERING";

        /// <summary>On a deed taken back from a ship being unregistered: the ship id.</summary>
        private const string DeedUnregisteredVariable = "SHIP_DEED_UNREGISTERED";

        /// <summary>Marks a module the ship is about to save as fitted and saves the character. Call after serializing it.</summary>
        public static void MarkModuleFitting(uint player, uint item, string shipId)
        {
            SetLocalString(item, ModuleFittingVariable, $"{shipId}|{GetObjectUUID(item)}");
            ExportSingleCharacter(player);
        }

        /// <summary>
        /// The ship saved the fitted module: its receipt follows, and outlives any later change to the ship until the
        /// fitter's next login has settled their marked modules (<see cref="ShipModuleReceipt"/>).
        /// </summary>
        public static void SaveModuleReceipt(uint player, string instanceId, string shipId)
        {
            DB.Set(new ShipModuleReceipt { Id = instanceId, OwnerPlayerId = GetObjectUUID(player), ShipId = shipId });
        }

        /// <summary>
        /// Saves the removal record of a module just taken into the remover's inventory and marks the item, before the ship
        /// saves the module as removed. The character is not saved yet: until <see cref="FinishModuleRemoval"/>, the record
        /// is what says the module left the ship. A stale record of the same module (a restart before its ship saved the
        /// removal) is replaced, since only a module the ship still has can be taken.
        /// </summary>
        public static void BeginModuleRemoval(uint player, uint item, string shipId, ShipStatus.ShipStatusModule module)
        {
            DB.Set(new ShipModuleRemoval
            {
                Id = module.ItemInstanceId,
                OwnerPlayerId = GetObjectUUID(player),
                ShipId = shipId,
                SerializedItem = module.SerializedItem
            });
            SetLocalString(item, ModuleRemovedVariable, $"{shipId}|{module.ItemInstanceId}");
        }

        /// <summary>The ship saved the module as removed: the character is saved holding it, then the record goes.</summary>
        public static void FinishModuleRemoval(uint player, uint item, string instanceId)
        {
            ExportSingleCharacter(player);
            DB.Delete<ShipModuleRemoval>(instanceId);
            DeleteLocalString(item, ModuleRemovedVariable);
        }

        /// <summary>Marks a deed about to be saved as a ship and saves the character. Call after serializing it.</summary>
        public static void MarkDeedRegistering(uint player, uint deed, string shipId)
        {
            SetLocalString(deed, DeedRegisteringVariable, shipId);
            ExportSingleCharacter(player);
        }

        /// <summary>Marks a deed taken back from a ship being unregistered and saves the character, before the ship goes.</summary>
        public static void MarkDeedUnregistered(uint player, uint deed, string shipId)
        {
            SetLocalString(deed, DeedUnregisteredVariable, shipId);
            ExportSingleCharacter(player);
        }

        /// <summary>The ship is gone: the deed's mark goes with the next save.</summary>
        public static void ClearDeedUnregistered(uint deed) => DeleteLocalString(deed, DeedUnregisteredVariable);

        /// <summary>
        /// Removes an unregistered ship: the owner's active ship, the ships docked aboard, its droids and listings, and its
        /// interior go first, the ship record last, so a restart part-way leaves a ship whose deed's login finishes this.
        /// </summary>
        public static void RemoveUnregisteredShip(string shipId)
        {
            var dbShip = DB.Get<PlayerShip>(shipId);
            if (dbShip == null)
                return;

            var dbPlayer = DB.Get<Player>(dbShip.OwnerPlayerId);
            if (dbPlayer != null && dbPlayer.ActiveShipId == shipId)
            {
                dbPlayer.ActiveShipId = Guid.Empty.ToString();
                DB.Set(dbPlayer);
            }

            UndockAllToStarport(dbShip, "was unregistered");
            ReturnShipDroidCrew(dbShip, "was unregistered");
            ShipTransfer.RemoveListings(shipId);

            var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
            if (dbProperty != null)
            {
                dbProperty.IsQueuedForDeletion = true;
                DB.Set(dbProperty);
            }

            DB.Delete<PlayerShip>(shipId);
        }

        /// <summary>Settles the ship module and deed moves a restart interrupted, from the marks on the player's items.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleShipItemMoves()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var settled = false;
            var removedHeld = new HashSet<string>();
            foreach (var item in GetAllItems(player))
            {
                if (TryReadShipMark(item, ModuleFittingVariable, out var shipId, out var instanceId))
                {
                    settled = true;
                    DeleteLocalString(item, ModuleFittingVariable);
                    if (DB.Get<ShipModuleReceipt>(instanceId) != null || FindFittedModule(DB.Get<PlayerShip>(shipId), instanceId) != null)
                    {
                        DestroyObject(item);
                        Log.Write(LogGroup.Space, $"{PlayerName.GetAuditName(player)} - removed module {instanceId} already fitted to ship {shipId} after a restart.");
                    }
                }

                if (TryReadShipMark(item, ModuleRemovedVariable, out _, out instanceId))
                {
                    settled = true;
                    DeleteLocalString(item, ModuleRemovedVariable);
                    removedHeld.Add(instanceId);
                }

                var registering = GetLocalString(item, DeedRegisteringVariable);
                if (!string.IsNullOrEmpty(registering))
                {
                    settled = true;
                    DeleteLocalString(item, DeedRegisteringVariable);
                    if (DB.Get<PlayerShip>(registering) != null)
                    {
                        DestroyObject(item);
                        Log.Write(LogGroup.Space, $"{PlayerName.GetAuditName(player)} - removed deed already registered as ship {registering} after a restart.");
                    }
                }

                var unregistered = GetLocalString(item, DeedUnregisteredVariable);
                if (!string.IsNullOrEmpty(unregistered))
                {
                    settled = true;
                    DeleteLocalString(item, DeedUnregisteredVariable);
                    var ship = DB.Get<PlayerShip>(unregistered);
                    if (ship != null && ship.OwnerPlayerId == playerId)
                    {
                        RemoveUnregisteredShip(unregistered);
                        Log.Write(LogGroup.Space, $"{PlayerName.GetAuditName(player)} - finished unregistering ship {unregistered} after a restart.");
                    }
                }
            }

            // The module removals a restart interrupted are settled from their records (ShipModuleRemovalRules).
            var givenAgain = SettleModuleRemovals(player, playerId, removedHeld);
            settled |= givenAgain.Count > 0;

            // The receipts go only once the character file is saved without the modules settled above (a destroyed
            // module is gone only after this script), so a restart before that save settles them again.
            var receiptQuery = new DBQuery<ShipModuleReceipt>()
                .AddFieldSearch(nameof(ShipModuleReceipt.OwnerPlayerId), playerId, false);
            var receiptCount = (int)DB.SearchCount(receiptQuery);
            var receiptIds = receiptCount <= 0
                ? new List<string>()
                : DB.Search(receiptQuery.AddPaging(receiptCount, 0)).Select(x => x.Id).ToList();
            if (!settled && receiptIds.Count == 0)
                return;

            DelayCommand(1f, () =>
            {
                if (!GetIsObjectValid(player))
                    return;

                ExportSingleCharacter(player);
                foreach (var receiptId in receiptIds)
                    DB.Delete<ShipModuleReceipt>(receiptId);
                foreach (var removalId in givenAgain)
                    DB.Delete<ShipModuleRemoval>(removalId);
            });
        }

        /// <summary>
        /// Settles the player's module removal records. A record whose module the character holds, or whose ship still has
        /// the module, is deleted. A module neither has is given again, marked; its record goes only after the delayed save
        /// (the ids returned), so a restart before it gives the module once more instead of losing it.
        /// </summary>
        private static List<string> SettleModuleRemovals(uint player, string playerId, HashSet<string> removedHeld)
        {
            var givenAgain = new List<string>();
            var query = new DBQuery<ShipModuleRemoval>()
                .AddFieldSearch(nameof(ShipModuleRemoval.OwnerPlayerId), playerId, false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return givenAgain;

            foreach (var removal in DB.Search(query.AddPaging(count, 0)).ToList())
            {
                var stillFitted = FindFittedModule(DB.Get<PlayerShip>(removal.ShipId), removal.Id) != null;
                switch (ShipModuleRemovalRules.Settle(removedHeld.Contains(removal.Id), stillFitted))
                {
                    case ShipModuleRemovalOutcome.Finished:
                    case ShipModuleRemovalOutcome.NeverHappened:
                        DB.Delete<ShipModuleRemoval>(removal.Id);
                        break;
                    case ShipModuleRemovalOutcome.GiveAgain:
                        var item = ObjectPlugin.Deserialize(removal.SerializedItem);
                        if (!Item.TryAcquireItem(player, item))
                        {
                            SendMessageToPC(player, "A ship module you removed before a restart is waiting for you. Make room in your inventory and log in again.");
                            break;
                        }

                        SetLocalString(item, ModuleRemovedVariable, $"{removal.ShipId}|{removal.Id}");
                        givenAgain.Add(removal.Id);
                        Log.Write(LogGroup.Space, $"{PlayerName.GetAuditName(player)} - gave back module {removal.Id} removed from ship {removal.ShipId} before a restart.");
                        break;
                }
            }

            return givenAgain;
        }

        private static bool TryReadShipMark(uint item, string variable, out string shipId, out string instanceId)
        {
            var mark = GetLocalString(item, variable).Split('|');
            shipId = mark.Length == 2 ? mark[0] : string.Empty;
            instanceId = mark.Length == 2 ? mark[1] : string.Empty;
            return !string.IsNullOrEmpty(shipId) && !string.IsNullOrEmpty(instanceId);
        }

        /// <summary>The ship's fitted module of this instance, with the slot map holding it, or null.</summary>
        private static (Dictionary<int, ShipStatus.ShipStatusModule> Modules, int Slot, ShipStatus.ShipStatusModule Module)? FindFittedModule(
            PlayerShip ship, string instanceId)
        {
            if (ship?.Status == null || string.IsNullOrEmpty(instanceId))
                return null;

            foreach (var modules in new[] { ship.Status.HighPowerModules, ship.Status.MidPowerModules, ship.Status.LowPowerModules, ship.Status.ConfigurationModules })
            {
                var fitted = modules?.FirstOrDefault(x => x.Value?.ItemInstanceId == instanceId);
                if (fitted?.Value != null)
                    return (modules, fitted.Value.Key, fitted.Value.Value);
            }

            return null;
        }

        /// <summary>Every item the creature carries, inside bags too. A container's items are listed before a bag is opened.</summary>
        private static List<uint> GetAllItems(uint container)
        {
            var items = new List<uint>();
            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
                items.Add(item);

            foreach (var bag in items.Where(GetHasInventory).ToList())
                items.AddRange(GetAllItems(bag));

            return items;
        }
    }
}
