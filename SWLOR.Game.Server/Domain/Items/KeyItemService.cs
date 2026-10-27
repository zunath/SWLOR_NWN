using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// The key items players collect: the logic and state that used to be static in <c>Service.KeyItem</c>, moved here
    /// unchanged. The facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class KeyItemService : IKeyItemService
    {
        // All categories/key items
        private readonly Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> _allCategories = new Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute>();
        private readonly Dictionary<KeyItemType, KeyItemAttribute> _allKeyItems = new Dictionary<KeyItemType, KeyItemAttribute>();
        private readonly Dictionary<KeyItemCategoryType, List<KeyItemType>> _allKeyItemsByCategory = new Dictionary<KeyItemCategoryType, List<KeyItemType>>();

        // Active categories/key items
        private readonly Dictionary<KeyItemType, KeyItemAttribute> _activeKeyItems = new Dictionary<KeyItemType, KeyItemAttribute>();
        private readonly Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> _activeKeyItemCategories = new Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute>();
        private readonly Dictionary<KeyItemCategoryType, Dictionary<KeyItemType, KeyItemAttribute>> _activeKeyItemsByCategory = new Dictionary<KeyItemCategoryType, Dictionary<KeyItemType, KeyItemAttribute>>();

        // By key item type name or Id
        private readonly Dictionary<string, KeyItemType> _keyItemsByTypeName = new Dictionary<string, KeyItemType>();
        private readonly Dictionary<int, KeyItemType> _keyItemsByTypeId = new Dictionary<int, KeyItemType>();

        private readonly IDBService _db;

        public KeyItemService(IDBService db)
        {
            _db = db;
        }

        public void LoadData()
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

        public KeyItemCategoryAttribute GetKeyItemCategory(KeyItemCategoryType type)
        {
            return _allCategories[type];
        }

        public KeyItemAttribute GetKeyItem(KeyItemType keyItem)
        {
            return _allKeyItems[keyItem];
        }

        public KeyItemType GetKeyItemTypeById(int keyItemId)
        {
            return !_keyItemsByTypeId.ContainsKey(keyItemId) ?
                KeyItemType.Invalid :
                _keyItemsByTypeId[keyItemId];
        }

        public KeyItemType GetKeyItemTypeByName(string name)
        {
            return !_keyItemsByTypeName.ContainsKey(name) ?
                KeyItemType.Invalid :
                _keyItemsByTypeName[name];
        }

        public Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> GetActiveCategories()
        {
            return _activeKeyItemCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        public Dictionary<KeyItemType, KeyItemAttribute> GetActiveKeyItemsByCategory(KeyItemCategoryType category)
        {
            if(!_activeKeyItemsByCategory.ContainsKey(category))
                return new Dictionary<KeyItemType, KeyItemAttribute>();

            return _activeKeyItemsByCategory[category].ToDictionary(s => s.Key, s => s.Value);
        }

        public void GiveKeyItem(uint player, KeyItemType keyItem)
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

        public void RemoveKeyItem(uint player, KeyItemType keyItem)
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

        public bool HasKeyItem(uint player, KeyItemType keyItem)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            return dbPlayer.KeyItems.ContainsKey(keyItem);
        }

        public bool HasAllKeyItems(uint player, List<KeyItemType> keyItems)
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

        public void ObtainKeyItem()
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
                AdjustVisibility(player, placeable, VisibilityType.Hidden);
            }
        }

        /// <summary>
        /// Hides the placeable from the player. The original call into <c>ObjectVisibility</c> (which is not converted yet
        /// and reads and saves the player through the static database), on its own so a unit test can answer it.
        /// </summary>
        protected virtual void AdjustVisibility(uint player, uint placeable, VisibilityType type)
        {
            ObjectVisibility.AdjustVisibility(player, placeable, type);
        }
    }
}
