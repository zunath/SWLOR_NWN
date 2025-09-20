using System;
using System.Collections.Generic;
using System.Linq;

using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Service
{
    public static class KeyItem
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        // All categories/key items
        private static readonly Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> _allCategories = new Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute>();
        private static readonly Dictionary<KeyItemType, KeyItemAttribute> _allKeyItems = new Dictionary<KeyItemType, KeyItemAttribute>();
        private static readonly Dictionary<KeyItemCategoryType, List<KeyItemType>> _allKeyItemsByCategory = new Dictionary<KeyItemCategoryType, List<KeyItemType>>();

        // Active categories/key items
        private static readonly Dictionary<KeyItemType, KeyItemAttribute> _activeKeyItems = new Dictionary<KeyItemType, KeyItemAttribute>();
        private static readonly Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> _activeKeyItemCategories = new Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute>();
        private static readonly Dictionary<KeyItemCategoryType, Dictionary<KeyItemType, KeyItemAttribute>> _activeKeyItemsByCategory = new Dictionary<KeyItemCategoryType, Dictionary<KeyItemType, KeyItemAttribute>>();

        // By key item type name or Id
        private static readonly Dictionary<string, KeyItemType> _keyItemsByTypeName = new Dictionary<string, KeyItemType>();
        private static readonly Dictionary<int, KeyItemType> _keyItemsByTypeId = new Dictionary<int, KeyItemType>();

        /// <summary>
        /// When the module loads, cache all key item data.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadData()
        {
            // Organize categories
            var categories = Enum.GetValues(typeof(KeyItemCategoryType)).Cast<KeyItemCategoryType>();
            foreach (var category in categories)
            {
                var categoryDetail = category.GetAttribute<KeyItemCategoryType, KeyItemCategoryAttribute>();
                _allCategories[category] = categoryDetail;
                _allKeyItemsByCategory[category] = new List<KeyItemType>();

                if (categoryDetail.IsActive)
                {
                    _activeKeyItemCategories[category] = categoryDetail;
                    _activeKeyItemsByCategory[category] = new Dictionary<KeyItemType, KeyItemAttribute>();
                }
            }

            // Organize key items
            var keyItems = Enum.GetValues(typeof(KeyItemType)).Cast<KeyItemType>();
            foreach (var keyItem in keyItems)
            {
                var keyItemDetail = keyItem.GetAttribute<KeyItemType, KeyItemAttribute>();
                _allKeyItems[keyItem] = keyItemDetail;
                _allKeyItemsByCategory[keyItemDetail.Category].Add(keyItem);

                var enumName = Enum.GetName(typeof(KeyItemType), keyItem);
                if (!string.IsNullOrWhiteSpace(enumName))
                {
                    _keyItemsByTypeName[enumName] = keyItem;
                }

                _keyItemsByTypeId[(int) keyItem] = keyItem;

                if (keyItemDetail.IsActive)
                {
                    _activeKeyItems[keyItem] = keyItemDetail;
                    if (_activeKeyItemsByCategory.ContainsKey(keyItemDetail.Category))
                    {
                        _activeKeyItemsByCategory[keyItemDetail.Category][keyItem] = keyItemDetail;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a key item category's detail by its type.
        /// </summary>
        /// <param name="type">The type of key item category to retrieve.</param>
        /// <returns>A key item category detail.</returns>
        public static KeyItemCategoryAttribute GetKeyItemCategory(KeyItemCategoryType type)
        {
            return _allCategories[type];
        }

        /// <summary>
        /// Gets a key item's detail by its type.
        /// </summary>
        /// <param name="keyItem">The type of key item to retrieve.</param>
        /// <returns>A key item detail</returns>
        public static KeyItemAttribute GetKeyItem(KeyItemType keyItem)
        {
            return _allKeyItems[keyItem];
        }

        /// <summary>
        /// Retrieves a key item type by its integer Id.
        /// Returns KeyItemType.Invalid if not found.
        /// </summary>
        /// <param name="keyItemId">The Id to search for</param>
        /// <returns>A KeyItemType matching the Id.</returns>
        public static KeyItemType GetKeyItemTypeById(int keyItemId)
        {
            return !_keyItemsByTypeId.ContainsKey(keyItemId) ? 
                KeyItemType.Invalid : 
                _keyItemsByTypeId[keyItemId];
        }

        /// <summary>
        /// Retrieves a key item type by its name.
        /// Returns KeyItemType.Invalid if not found.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>A KeyItemType matching the name.</returns>
        public static KeyItemType GetKeyItemTypeByName(string name)
        {
            return !_keyItemsByTypeName.ContainsKey(name) ? 
                KeyItemType.Invalid : 
                _keyItemsByTypeName[name];
        }

        /// <summary>
        /// Retrieves all of the active key item categories.
        /// </summary>
        /// <returns>All active key item categories</returns>
        public static Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> GetActiveCategories()
        {
            return _activeKeyItemCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves all of the active key items contained in a specific category.
        /// </summary>
        /// <param name="category">The category to search by</param>
        /// <returns>A dictionary containing key item type and key item attribute data.</returns>
        public static Dictionary<KeyItemType, KeyItemAttribute> GetActiveKeyItemsByCategory(KeyItemCategoryType category)
        {
            if(!_activeKeyItemsByCategory.ContainsKey(category))
                return new Dictionary<KeyItemType, KeyItemAttribute>();

            return _activeKeyItemsByCategory[category].ToDictionary(s => s.Key, s => s.Value);
        }

        /// <summary>
        /// Gives a specific key item to a player.
        /// If player is not a PC or is a DM, nothing will happen.
        /// If player already has the key item, nothing will happen.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="keyItem">The key item type to give.</param>
        public static void GiveKeyItem(uint player, KeyItemType keyItem)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer.KeyItems.ContainsKey(keyItem))
                return;

            dbPlayer.KeyItems[keyItem] = DateTime.UtcNow;
            _db.Set(dbPlayer);

            var keyItemDetail = _allKeyItems[keyItem];
            SendMessageToPC(player, $"You acquire the '{keyItemDetail.Name}' key item.");
            Gui.PublishRefreshEvent(player, new KeyItemReceivedRefreshEvent(keyItem));
        }

        /// <summary>
        /// Removes a key item from a player.
        /// If player is not a PC or is a DM, nothing will happen.
        /// If player does not have the key item, nothing will happen.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="keyItem">The key item type to remove.</param>
        public static void RemoveKeyItem(uint player, KeyItemType keyItem)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.KeyItems.ContainsKey(keyItem))
                return;

            dbPlayer.KeyItems.Remove(keyItem);
            _db.Set(dbPlayer);

            var keyItemDetail = _allKeyItems[keyItem];
            SendMessageToPC(player, $"You lost the '{keyItemDetail.Name}' key item.");
        }

        /// <summary>
        /// Checks whether a player has a key item.
        /// Returns false if player is non-PC or DM.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="keyItem">The type of key item to check for.</param>
        /// <returns>true if the player has a key item, false otherwise</returns>
        public static bool HasKeyItem(uint player, KeyItemType keyItem)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            return dbPlayer.KeyItems.ContainsKey(keyItem);
        }

        /// <summary>
        /// Checks whether a player has all of the specified key items.
        /// Returns false if player is non-PC or DM.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="keyItems">Required key items.</param>
        /// <returns>true if player has all key items, false otherwise</returns>
        public static bool HasAllKeyItems(uint player, List<KeyItemType> keyItems)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return false;

            // No key items specified, default to true.
            if (keyItems == null)
                return true;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            
            foreach (var ki in keyItems)
            {
                if (!dbPlayer.KeyItems.ContainsKey(ki))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// When a placeable with a key item defined is used by a player, give it to them.
        /// </summary>
        [ScriptHandler(ScriptName.OnGetKeyItem)]
        public static void ObtainKeyItem()
        {

            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var placeable = OBJECT_SELF;
            var keyItemID = GetLocalInt(placeable, "KEY_ITEM_ID");

            if (keyItemID <= 0) return;

            var keyItem = (KeyItemType) keyItemID;
            if (HasKeyItem(player, keyItem))
            {
                SendMessageToPC(player, "You already have this key item.");
                return;
            }

            GiveKeyItem(player, keyItem);

            var visibilityGUID = GetLocalString(placeable, "VISIBILITY_OBJECT_ID");
            if (!string.IsNullOrWhiteSpace(visibilityGUID))
            {
                ObjectVisibility.AdjustVisibility(player, placeable, VisibilityType.Hidden);
            }
        }

    }
}
