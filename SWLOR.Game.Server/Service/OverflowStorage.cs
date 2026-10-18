using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Items a character upgrade could not keep in a character's inventory. They wait in the database until the
    /// player claims them with /overflow.
    /// </summary>
    public static class OverflowStorage
    {
        /// <summary>
        /// Marks a claimed item with its record until that record is deleted. The item is saved with the character
        /// before the record is deleted, so after a crash between the two, the next claim or login finds the mark
        /// and deletes the record instead of giving the item again. Character files keep item locals only.
        /// </summary>
        private const string ItemIdVariable = "OVERFLOW_ITEM_ID";

        public static long GetItemCount(string playerId)
        {
            return DB.SearchCount(BuildQuery(playerId));
        }

        /// <summary>Moves overflow items into the player's inventory until it is full.</summary>
        public static void Claim(uint player)
        {
            var playerId = GetObjectUUID(player);
            DeleteClaimedRecords(player);
            var records = DB.Search(BuildQuery(playerId)).ToList();
            if (records.Count == 0)
            {
                SendMessageToPC(player, "You have no items in overflow storage.");
                return;
            }

            var claimed = new List<(OverflowItem Record, uint Item)>();
            foreach (var record in records)
            {
                var item = ObjectPlugin.Deserialize(record.Data);
                if (!GetIsObjectValid(item))
                {
                    Log.Write(LogGroup.Migration, $"Overflow item '{record.Id}' for player [{playerId}] could not be restored.");
                    continue;
                }
                SetLocalString(item, ItemIdVariable, record.Id);
                // A smaller item may still fit where this one does not.
                if (!FitsInInventory(player, item) || !ObjectPlugin.AcquireItem(player, item))
                {
                    DestroyObject(item);
                    continue;
                }
                claimed.Add((record, item));
            }

            if (claimed.Count == 0)
            {
                SendMessageToPC(player, $"Your inventory is full. Free inventory space, then use /overflow again to claim your {records.Count} item(s).");
                return;
            }

            if (!SaveCharacter(player))
            {
                foreach (var (_, item) in claimed)
                    DestroyObject(item);
                SendMessageToPC(player, "Your character could not be saved, so your items stay in overflow storage. Please try again.");
                return;
            }

            foreach (var (record, item) in claimed)
            {
                DB.Delete<OverflowItem>(record.Id);
                if (GetIsObjectValid(item))
                    DeleteLocalString(item, ItemIdVariable);
                Log.WriteStructured(
                    LogGroup.Migration,
                    "Overflow claim: PlayerId={PlayerId} OverflowItemId={OverflowItemId} Resref={Resref} Quantity={Quantity}",
                    playerId,
                    record.Id,
                    record.Resref,
                    record.Quantity);
            }

            var remaining = GetItemCount(playerId);
            if (remaining <= 0)
                SendMessageToPC(player, $"You claimed {claimed.Count} item(s) from overflow storage.");
            else
                SendMessageToPC(player, $"You claimed {claimed.Count} item(s) from overflow storage. {remaining} item(s) remain; free inventory space and use /overflow again.");
        }

        /// <summary>Tells the player at login how many items are waiting in overflow storage.</summary>
        public static void NotifyOnLogin(uint player)
        {
            DeleteClaimedRecords(player);
            var count = GetItemCount(GetObjectUUID(player));
            if (count > 0)
                SendMessageToPC(player, ColorToken.Green($"{count} item(s) from a character update did not fit in your inventory and are in overflow storage. Free inventory space and use /overflow to claim them."));
        }

        /// <summary>Deletes the records of claimed items the character already holds, and clears their marks.</summary>
        private static void DeleteClaimedRecords(uint player)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
                DeleteClaimedRecord(item);
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                DeleteClaimedRecord(GetItemInSlot((InventorySlot)slot, player));
        }

        private static void DeleteClaimedRecord(uint item)
        {
            var id = GetLocalString(item, ItemIdVariable);
            if (string.IsNullOrEmpty(id))
                return;
            DB.Delete<OverflowItem>(id);
            DeleteLocalString(item, ItemIdVariable);
        }

        /// <summary>
        /// True when the engine finds room for the item in the character's own inventory grid. Acquiring an item that
        /// has no room can remove items the character already carries, so a claim never tries one.
        /// </summary>
        private static unsafe bool FitsInInventory(uint player, uint item)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var creature = server.GetGameObject(player)?.AsNWSCreature();
            var native = server.GetItemByGameObjectID(item);
            if (creature?.m_pcItemRepository == null || native == null)
                return false;
            byte x = 0, y = 0;
            return creature.m_pcItemRepository.FindPosition(native, &x, &y) != 0;
        }

        /// <summary>Saves the character now. The ordinary export command queues a later save.</summary>
        private static bool SaveCharacter(uint player)
        {
            try
            {
                var client = NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByObjectId(player);
                return client != null && client.SaveServerCharacter() != 0;
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Migration, $"Overflow claim could not save player [{GetObjectUUID(player)}]: {ex.Message}");
                return false;
            }
        }

        private static DBQuery<OverflowItem> BuildQuery(string playerId)
        {
            return new DBQuery<OverflowItem>()
                .AddFieldSearch(nameof(OverflowItem.PlayerId), playerId, false);
        }
    }
}
