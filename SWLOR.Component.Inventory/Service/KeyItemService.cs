using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Inventory;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Inventory.Service
{
    public class KeyItemService : IKeyItemService
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private IEnumCache<KeyItemType, KeyItemAttribute> _keyItemCache;
        private IEnumCache<KeyItemCategoryType, KeyItemCategoryAttribute> _categoryCache;
        private Dictionary<string, KeyItemType> _keyItemsByTypeName;
        private Dictionary<int, KeyItemType> _keyItemsByTypeId;

        public KeyItemService(
            IDatabaseService db,
            IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _cacheService = new Lazy<IGenericCacheService>(() => _serviceProvider.GetRequiredService<IGenericCacheService>());
            _guiService = new Lazy<IGuiService>(() => _serviceProvider.GetRequiredService<IGuiService>());
            _objectVisibilityService = new Lazy<IObjectVisibilityService>(() => _serviceProvider.GetRequiredService<IObjectVisibilityService>());
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IGenericCacheService> _cacheService;
        private readonly Lazy<IGuiService> _guiService;
        private readonly Lazy<IObjectVisibilityService> _objectVisibilityService;
        
        private IGenericCacheService CacheService => _cacheService.Value;
        private IGuiService GuiService => _guiService.Value;
        private IObjectVisibilityService ObjectVisibilityService => _objectVisibilityService.Value;

        /// <summary>
        /// When the module loads, cache all key item data.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadData()
        {
            // Build key item cache with all items and filtered caches
            _keyItemCache = CacheService
                .BuildEnumCache<KeyItemType, KeyItemAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", x => x.IsActive)
                .WithGroupedCache("ByCategory", x => x.Category)
                .WithFilteredGroupedCache("ActiveByCategory", x => x.IsActive, x => x.Category)
                .Build();

            // Build category cache with all items and filtered caches
            _categoryCache = CacheService
                .BuildEnumCache<KeyItemCategoryType, KeyItemCategoryAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", x => x.IsActive)
                .Build();

            // Build lookup dictionaries
            _keyItemsByTypeName = new Dictionary<string, KeyItemType>();
            _keyItemsByTypeId = new Dictionary<int, KeyItemType>();

            foreach (var keyItem in _keyItemCache.AllItems.Keys)
            {
                var enumName = Enum.GetName(typeof(KeyItemType), keyItem);
                if (!string.IsNullOrWhiteSpace(enumName))
                {
                    _keyItemsByTypeName[enumName] = keyItem;
                }
                _keyItemsByTypeId[(int)keyItem] = keyItem;
            }
        }

        /// <summary>
        /// Gets a key item category's detail by its type.
        /// </summary>
        /// <param name="type">The type of key item category to retrieve.</param>
        /// <returns>A key item category detail.</returns>
        public KeyItemCategoryAttribute GetKeyItemCategory(KeyItemCategoryType type)
        {
            return _categoryCache!.AllItems[type];
        }

        /// <summary>
        /// Gets a key item's detail by its type.
        /// </summary>
        /// <param name="keyItem">The type of key item to retrieve.</param>
        /// <returns>A key item detail</returns>
        public KeyItemAttribute GetKeyItem(KeyItemType keyItem)
        {
            return _keyItemCache!.AllItems[keyItem];
        }

        /// <summary>
        /// Retrieves a key item type by its integer Id.
        /// Returns KeyItemType.Invalid if not found.
        /// </summary>
        /// <param name="keyItemId">The Id to search for</param>
        /// <returns>A KeyItemType matching the Id.</returns>
        public KeyItemType GetKeyItemTypeById(int keyItemId)
        {
            return !_keyItemsByTypeId!.ContainsKey(keyItemId) ? 
                KeyItemType.Invalid : 
                _keyItemsByTypeId[keyItemId];
        }

        /// <summary>
        /// Retrieves a key item type by its name.
        /// Returns KeyItemType.Invalid if not found.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>A KeyItemType matching the name.</returns>
        public KeyItemType GetKeyItemTypeByName(string name)
        {
            return !_keyItemsByTypeName!.ContainsKey(name) ? 
                KeyItemType.Invalid : 
                _keyItemsByTypeName[name];
        }

        /// <summary>
        /// Retrieves all of the active key item categories.
        /// </summary>
        /// <returns>All active key item categories</returns>
        public Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> GetActiveCategories()
        {
            return _categoryCache!.GetFilteredCache("Active")!.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves all of the active key items contained in a specific category.
        /// </summary>
        /// <param name="category">The category to search by</param>
        /// <returns>A dictionary containing key item type and key item attribute data.</returns>
        public Dictionary<KeyItemType, KeyItemAttribute> GetActiveKeyItemsByCategory(KeyItemCategoryType category)
        {
            var activeByCategory = _keyItemCache!.GetFilteredGroupedCache<KeyItemCategoryType>("ActiveByCategory");
            if (activeByCategory == null || !activeByCategory.ContainsKey(category))
                return new Dictionary<KeyItemType, KeyItemAttribute>();

            return activeByCategory[category].ToDictionary(s => s.Key, s => s.Value);
        }

        /// <summary>
        /// Gives a specific key item to a player.
        /// If player is not a PC or is a DM, nothing will happen.
        /// If player already has the key item, nothing will happen.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="keyItem">The key item type to give.</param>
        public void GiveKeyItem(uint player, KeyItemType keyItem)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer.KeyItems.ContainsKey(keyItem))
                return;

            dbPlayer.KeyItems[keyItem] = DateTime.UtcNow;
            _db.Set(dbPlayer);

            var keyItemDetail = _keyItemCache!.AllItems[keyItem];
            SendMessageToPC(player, $"You acquire the '{keyItemDetail.Name}' key item.");
            GuiService.PublishRefreshEvent(player, new KeyItemReceivedRefreshEvent(keyItem));
        }

        /// <summary>
        /// Removes a key item from a player.
        /// If player is not a PC or is a DM, nothing will happen.
        /// If player does not have the key item, nothing will happen.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="keyItem">The key item type to remove.</param>
        public void RemoveKeyItem(uint player, KeyItemType keyItem)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.KeyItems.ContainsKey(keyItem))
                return;

            dbPlayer.KeyItems.Remove(keyItem);
            _db.Set(dbPlayer);

            var keyItemDetail = _keyItemCache!.AllItems[keyItem];
            SendMessageToPC(player, $"You lost the '{keyItemDetail.Name}' key item.");
        }

        /// <summary>
        /// Checks whether a player has a key item.
        /// Returns false if player is non-PC or DM.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="keyItem">The type of key item to check for.</param>
        /// <returns>true if the player has a key item, false otherwise</returns>
        public bool HasKeyItem(uint player, KeyItemType keyItem)
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

        /// <summary>
        /// When a placeable with a key item defined is used by a player, give it to them.
        /// </summary>
        [ScriptHandler<OnGetKeyItem>]
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
                ObjectVisibilityService.AdjustVisibility(player, placeable, VisibilityType.Hidden);
            }
        }
    }
}
