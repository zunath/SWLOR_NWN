using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
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
        /// The claimed items whose records may still exist, saved with the character before those records are
        /// deleted. A crash between the two leaves the list in the character file, and the next claim or login
        /// deletes the records instead of giving the items again.
        /// </summary>
        private const string ClaimedVariable = "OVERFLOW_CLAIMED_IDS";

        public static long GetItemCount(string playerId)
        {
            return DB.SearchCount(BuildQuery(playerId));
        }

        /// <summary>Moves overflow items into the player's inventory until it is full.</summary>
        public static void Claim(uint player)
        {
            var playerId = GetObjectUUID(player);
            DeleteClaimedRecords(player);
            var items = DB.Search(BuildQuery(playerId)).ToList();
            if (items.Count == 0)
            {
                SendMessageToPC(player, "You have no items in overflow storage.");
                return;
            }

            var claimed = new List<OverflowItem>();
            foreach (var dbItem in items)
            {
                var item = ObjectPlugin.Deserialize(dbItem.Data);
                if (!GetIsObjectValid(item))
                {
                    Log.Write(LogGroup.Migration, $"Overflow item '{dbItem.Id}' for player [{playerId}] could not be restored.");
                    continue;
                }
                if (!ObjectPlugin.AcquireItem(player, item))
                {
                    DestroyObject(item);
                    break;
                }
                claimed.Add(dbItem);
            }

            // Until the character is saved, the claimed records stay and the next claim or login deletes them.
            var pending = 0;
            if (claimed.Count > 0)
            {
                SetLocalString(player, ClaimedVariable, JsonConvert.SerializeObject(claimed.Select(x => x.Id)));
                if (SaveCharacter(player))
                    DeleteClaimedRecords(player);
                else
                    pending = claimed.Count;
                foreach (var dbItem in claimed)
                {
                    Log.WriteStructured(
                        LogGroup.Migration,
                        "Overflow claim: PlayerId={PlayerId} OverflowItemId={OverflowItemId} Resref={Resref} Quantity={Quantity}",
                        playerId,
                        dbItem.Id,
                        dbItem.Resref,
                        dbItem.Quantity);
                }
            }

            var remaining = GetItemCount(playerId) - pending;
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

        /// <summary>Deletes the records of items the character already holds, then forgets them.</summary>
        private static void DeleteClaimedRecords(uint player)
        {
            var data = GetLocalString(player, ClaimedVariable);
            if (string.IsNullOrEmpty(data))
                return;
            foreach (var id in JsonConvert.DeserializeObject<List<string>>(data))
                DB.Delete<OverflowItem>(id);
            DeleteLocalString(player, ClaimedVariable);
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
