using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The inventory and overflow-storage work that livestock and farmhands share: counting and taking items, and giving
    /// items to a character, with whatever does not fit going to their overflow storage (/overflow). The records are written
    /// first and the items second wherever a stop between the two could repeat a gift, so a restart can lose an item but never
    /// duplicate one.
    /// </summary>
    public static class FarmItems
    {
        /// <summary>How many of an item the player carries, stack sizes included. Bags are not searched.</summary>
        public static int Count(uint player, string resref)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref)
                    count += GetItemStackSize(item);
            }

            return count;
        }

        /// <summary>Takes up to <paramref name="count"/> of an item from the player's inventory. Returns how many it took.</summary>
        public static int Take(uint player, string resref, int count)
        {
            var taken = 0;
            var items = new List<uint>();
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref)
                    items.Add(item);
            }

            foreach (var item in items)
            {
                if (taken >= count)
                    break;

                var size = GetItemStackSize(item);
                var use = System.Math.Min(size, count - taken);
                if (use >= size)
                    DestroyObject(item);
                else
                    SetItemStackSize(item, size - use);
                taken += use;
            }

            return taken;
        }

        /// <summary>
        /// Gives a player <paramref name="count"/> of an item: into their inventory while there is room, and the rest into
        /// their overflow storage. Returns how many went to overflow storage.
        /// </summary>
        /// <param name="player">The player, or invalid to send everything to overflow storage.</param>
        /// <param name="where">The place an item is created at before it is saved to overflow storage.</param>
        public static int Give(uint player, string playerId, string resref, int count, Location where)
        {
            var overflow = 0;
            for (var unit = 0; unit < count; unit++)
            {
                if (GetIsObjectValid(player) && TryPutInInventory(player, resref))
                    continue;

                if (StoreInOverflow(playerId, resref, where))
                    overflow++;
            }

            return overflow;
        }

        /// <summary>
        /// Creates one item in the player's inventory. Returns false, leaving nothing behind, when it did not arrive there: the
        /// inventory count decides, since an item that merged into a stack can come back as an invalid handle.
        /// </summary>
        private static bool TryPutInInventory(uint player, string resref)
        {
            var before = Count(player, resref);
            var item = CreateItemOnObject(resref, player);
            if (Count(player, resref) > before)
                return true;

            if (GetIsObjectValid(item))
                DestroyObject(item);
            return false;
        }

        /// <summary>Saves a new item of the blueprint to the player's overflow storage. False when the blueprint does not exist.</summary>
        public static bool StoreInOverflow(string playerId, string resref, Location where)
        {
            var item = CreateObject(ObjectType.Item, resref, where);
            if (!GetIsObjectValid(item))
            {
                Log.Write(LogGroup.Property, $"Farm item '{resref}' for player {playerId} could not be created, so it is lost.");
                return false;
            }

            StoreInOverflow(playerId, item, string.Empty);
            return true;
        }

        /// <summary>
        /// Saves an item to the player's overflow storage and destroys it. A record id makes the save repeatable: a return that a
        /// restart interrupts writes the same record again instead of a second one.
        /// </summary>
        public static void StoreInOverflow(string playerId, uint item, string recordId)
        {
            var record = new OverflowItem
            {
                PlayerId = playerId,
                Name = GetName(item),
                Resref = GetResRef(item),
                Tag = GetTag(item),
                Quantity = 1,
                Data = ObjectPlugin.Serialize(item)
            };
            if (!string.IsNullOrEmpty(recordId))
                record.Id = recordId;

            DB.Set(record);
            DestroyObject(item);
        }
    }
}
