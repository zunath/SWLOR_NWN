using System;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using Item = SWLOR.Game.Server.Service.Item;
using Object = SWLOR.Game.Server.Core.NWNX.Object;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class PersistentStorage
    {
        /// <summary>
        /// When a player adds an item to a public storage placeable,
        /// store that item in the database.
        /// </summary>
        [NWNEventHandler("inv_add_bef")]
        public static void AddItemToPublicStorage()
        {
            var container = OBJECT_SELF;
            if (GetResRef(container) != "public_p_storage" || GetLocalBool(container, "IS_DESERIALIZING")) return;

            var storageId = GetStorageID();
            var key = $"PublicStorage:{storageId}";
            AddItem(storageId, "PublicStorage");
        }

        /// <summary>
        /// When a player removes an item from a public storage placeable,
        /// remove that item from the database.
        /// </summary>
        [NWNEventHandler("storage_disturb")]
        public static void RemoveItemFromPublicStorage()
        {
            var storageId = GetStorageID();
            RemoveItem(storageId, "PublicStorage");
        }

        /// <summary>
        /// When a player opens a public storage chest,
        /// load all items into the placeable's inventory.
        /// </summary>
        [NWNEventHandler("storage_open")]
        public static void OpenStorage()
        {
            var storageId = GetStorageID();
            OpenStorage(storageId, "PublicStorage");
        }

        /// <summary>
        /// When a player closes a storage chest (whether public or bank),
        /// destroy all items in the container and unlock
        /// </summary>
        [NWNEventHandler("storage_close")]
        public static void CloseStorage()
        {
            var container = OBJECT_SELF;
            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            {
                DestroyObject(item);
            }

            SetLocked(container, false);
        }

        /// <summary>
        /// When a player adds an item to a bank chest,
        /// store it in the database.
        /// </summary>
        [NWNEventHandler("inv_add_bef")]
        public static void AddItemToBankStorage()
        {
            var container = OBJECT_SELF;
            if (GetResRef(container) != "bank_chest" || GetLocalBool(container, "IS_DESERIALIZING")) return;

            var item = StringToObject(Events.GetEventData("ITEM"));
            var player = GetItemPossessor(item);
            var playerId = GetObjectUUID(player);
            var storageId = GetStorageID();
            var key = $"{storageId}:{playerId}";
            AddItem(key, "Bank");
        }

        /// <summary>
        /// When a player removes an item from a bank chest,
        /// remove it from the database.
        /// </summary>
        [NWNEventHandler("bank_disturb")]
        public static void RemoveItemFromBankStorage()
        {
            var item = GetInventoryDisturbItem();
            var player = GetItemPossessor(item);
            var playerId = GetObjectUUID(player);
            var storageId = GetStorageID();
            var key = $"{storageId}:{playerId}";
            RemoveItem(key, "Bank");
        }

        /// <summary>
        /// When a player opens a bank chest,
        /// load all items associated to that bank and player into the inventory of the placeable.
        /// </summary>
        [NWNEventHandler("bank_open")]
        public static void OpenBank()
        {
            var player = GetLastOpenedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var storageId = GetStorageID();
            var key = $"{storageId}:{playerId}";
            OpenStorage(key, "Bank");
        }

        /// <summary>
        /// Retrieves the STORAGE_ID variable from the container.
        /// </summary>
        /// <returns>The value of the STORAGE_ID local variable</returns>
        protected static string GetStorageID()
        {
            var container = OBJECT_SELF;

            var storageID = GetLocalString(container, "STORAGE_ID");

            if (string.IsNullOrWhiteSpace(storageID))
                throw new Exception($"Container {GetName(container)} does not have a STORAGE_ID variable assigned.");

            return storageID;
        }

        /// <summary>
        /// Retrieves the STORAGE_ITEM_LIMIT variable from the container.
        /// </summary>
        /// <returns>The value of the STORAGE_ITEM_LIMIT local variable</returns>
        protected static int GetItemLimit()
        {
            var limit = GetLocalInt(OBJECT_SELF, "STORAGE_ITEM_LIMIT");
            if (limit <= 0)
                limit = 20;

            return limit;
        }

        /// <summary>
        /// Sends a message to a player informing them of the current number of items in the container and the maximum allowed.
        /// If incrementByOne is true, the current count will be increased by one. This is to account for the fact that
        /// the OnAddItem event fires before the item is actually added to the inventory (therefore it would have an off-by-one error)
        /// </summary>
        /// <param name="player">The player receiving the message</param>
        /// <param name="incrementByOne">Increments current item count by one if true, else does nothing.</param>
        protected static void SendItemLimitMessage(uint player, bool incrementByOne)
        {
            var container = OBJECT_SELF;
            var limit = GetItemLimit();
            var count = Item.GetInventoryItemCount(container);

            // The Add event fires before the item exists in the container. Need to increment by one in this scenario.
            if (incrementByOne)
                count++;

            SendMessageToPC(player, ColorToken.White("Item Limit: " + (count > limit ? limit : count) + " / ") + ColorToken.Red("" + limit));
        }

        /// <summary>
        /// Gets or sets the IS_LOADING local variable on the container.
        /// This is used to ensure the OnAddItem event does not process when the container is loading its items.
        /// </summary>
        protected static bool IsLoading
        {
            get => GetLocalInt(OBJECT_SELF, "IS_LOADING") == 1;
            set => SetLocalInt(OBJECT_SELF, "IS_LOADING", value ? 1 : 0);
        }

        /// <summary>
        /// Adds an item to the database under the specified key.
        /// </summary>
        /// <param name="key">The unique identifier under which this item list will be stored.</param>
        /// <param name="keyPrefix">The prefix to store the data under.</param>
        protected static void AddItem(string key, string keyPrefix)
        {
            // We don't want to serialize the item if we're loading its inventory.
            if (IsLoading) return;

            var container = OBJECT_SELF;
            var item = StringToObject(Events.GetEventData("ITEM"));
            var player = GetItemPossessor(item);
            var limit = GetItemLimit();
            var count = Item.GetInventoryItemCount(container);

            if (!GetIsPC(player) || GetIsDM(player))
            {
                CancelEvent(player, "Only players may store items here.");
                return;
            }

            if (GetHasInventory(item))
            {
                CancelEvent(player, "Containers cannot be stored.");
                return;
            }

            if (count >= limit)
            {
                CancelEvent(player, "No more items can be placed inside.");
                return;
            }

            if (GetBaseItemType(item) == BaseItem.Gold)
            {
                CancelEvent(player, "Credits cannot be placed inside.");
                return;
            }

            var items = DB.GetList<InventoryItem>(key, keyPrefix) ?? new EntityList<InventoryItem>();
            var itemID = Guid.Parse(GetObjectUUID(item));
            var data = Object.Serialize(item);

            items.Add(new InventoryItem
            {
                ID = itemID,
                Data = data,
                Name = GetName(item),
                Quantity = GetItemStackSize(item),
                Resref = GetResRef(item),
                Tag = GetTag(item)
            });

            DB.SetList(key, items, keyPrefix);
            SendItemLimitMessage(player, true);
        }

        /// <summary>
        /// Skips an event and sends a message to the player.
        /// Refer to NWNXEvents for information on which events may be skipped.
        /// Generally this is used in the OnAddItem event.
        /// </summary>
        /// <param name="player">The player who will receive the message.</param>
        /// <param name="message">The message sent</param>
        private static void CancelEvent(uint player, string message)
        {
            Events.SkipEvent();
            SendMessageToPC(player, message);
        }

        /// <summary>
        /// Removes an item from the database by the specified key.
        /// </summary>
        /// <param name="key">The unique identifier for this item list.</param>
        /// <param name="keyPrefix">The key prefix to remove from.</param>
        protected static void RemoveItem(string key, string keyPrefix)
        {
            var player = GetLastDisturbed();
            var type = GetInventoryDisturbType();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (type != DisturbType.Removed) return;

            var playerID = GetObjectUUID(player);

            var storageID = GetStorageID();
            var items = DB.GetList<InventoryItem>(key, keyPrefix);
            var item = GetInventoryDisturbItem();
            var itemID = Guid.Parse(GetObjectUUID(item));
            var existing = items.FirstOrDefault(x => x.ID == itemID);
            if (existing == null)
                throw new Exception($"Could not locate item with ID '{itemID} from database for storage '{storageID}', player ID '{playerID}' and itemID '{itemID}'");

            items.Remove(existing);
            DB.SetList(key, items, keyPrefix);
            SendItemLimitMessage(player, false);
        }

        /// <summary>
        /// Handles loading items into the container's inventory.
        /// </summary>
        /// <param name="key">The unique identifier under which this container's items are stored.</param>
        /// <param name="keyPrefix">The key prefix to look for this data under.</param>
        protected static void OpenStorage(string key, string keyPrefix)
        {
            var container = OBJECT_SELF;

            SetLocalBool(container, "IS_DESERIALIZING", true);
            var player = GetLastOpenedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var items = DB.GetList<InventoryItem>(key, keyPrefix) ?? new EntityList<InventoryItem>();

            // Prevent the OnAddItem event from firing while we're loading the inventory.
            IsLoading = true;
            foreach (var entity in items)
            {
                var deserializedItem = Object.Deserialize(entity.Data);
                Object.AcquireItem(container, deserializedItem);
            }

            IsLoading = false;

            SetLocked(container, true);
            SendMessageToPC(player, "Move away from the container to close it.");
            DeleteLocalBool(container, "IS_DESERIALIZING");
        }
    }
}
