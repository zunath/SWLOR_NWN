using SWLOR.Component.Market.Contracts;
using SWLOR.Component.Market.Entity;
using SWLOR.Component.Market.Enums;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Fishing.Contracts;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using MarketCategoryType = SWLOR.Component.Market.Enums.MarketCategoryType;

namespace SWLOR.Component.Market.Service
{
    public class PlayerMarket: IPlayerMarketService
    {
        private readonly IDatabaseService _db;
        private readonly IGenericCacheService _cacheService;
        private readonly IItemService _itemService;
        private readonly ICraftService _craftService;
        private readonly ISpaceService _spaceService;
        private readonly IPropertyService _propertyService;
        private readonly IFishingService _fishingService;
        private readonly IBeastMasteryService _beastMasteryService;
        
        public const int MaxListingsPerMarket = 25;
        
        // Cached data
        private IEnumCache<MarketCategoryType, MarketCategoryAttribute>? _marketCategoryCache;
        private IEnumCache<MarketRegionType, MarketRegionAttribute>? _marketRegionCache;
        
        // Additional caches for backward compatibility
        private readonly Dictionary<MarketCategoryType, MarketCategoryAttribute> _activeMarketCategories = new();
        
        // Pre-computed cache for fast retrieval
        private readonly Dictionary<MarketCategoryType, MarketCategoryAttribute> _allActiveMarketCategories = new();

        public PlayerMarket(
            IDatabaseService db, 
            IGenericCacheService cacheService,
            IItemService itemService,
            ICraftService craftService,
            ISpaceService spaceService,
            IPropertyService propertyService,
            IFishingService fishingService,
            IBeastMasteryService beastMasteryService)
        {
            _db = db;
            _cacheService = cacheService;
            _itemService = itemService;
            _craftService = craftService;
            _spaceService = spaceService;
            _propertyService = propertyService;
            _fishingService = fishingService;
            _beastMasteryService = beastMasteryService;
        }


        public void RemoveOldListings()
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true);
            var count = (int)_db.SearchCount(query);
            var listings = _db.Search(query
                .AddPaging(count, 0));
            var now = DateTime.UtcNow;

            foreach (var listing in listings)
            {
                if (listing.DateListed != null && Math.Abs(now.Subtract((DateTime)listing.DateListed).Days) >= 14)
                {
                    listing.IsListed = false;

                    _db.Set(listing);
                }
            }
        }

        public void CheckMarketTill()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer.MarketTill == 1)
            {
                SendMessageToPC(player, $"1 credit is in your market till.");
            }
            else if (dbPlayer.MarketTill > 1)
            {
                SendMessageToPC(player, $"{dbPlayer.MarketTill} credits are in your market till.");
            }
        }

        /// <summary>
        /// Reads all of the MarketCategoryType enumerations and adds them to the related dictionaries.
        /// </summary>
        public void LoadMarketCategories()
        {
            _marketCategoryCache = _cacheService.BuildEnumCache<MarketCategoryType, MarketCategoryAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", c => c.IsActive)
                .Build();

            // Populate the _activeMarketCategories dictionary for backward compatibility
            var activeCategories = _marketCategoryCache.GetFilteredCache("Active");
            if (activeCategories != null)
            {
                foreach (var (categoryType, categoryAttribute) in activeCategories)
                {
                    _activeMarketCategories[categoryType] = categoryAttribute;
                    _allActiveMarketCategories[categoryType] = categoryAttribute;
                }
            }
        }

        /// <summary>
        /// Reads all of the MarketRegionType enumerations and adds them to the related dictionaries.
        /// </summary>
        public void LoadMarkets()
        {
            _marketRegionCache = _cacheService.BuildEnumCache<MarketRegionType, MarketRegionAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", r => r.IsActive)
                .Build();
        }

        /// <summary>
        /// Retrieves all active market categories.
        /// </summary>
        /// <returns>A dictionary of active market categories.</returns>
        public Dictionary<MarketCategoryType, MarketCategoryAttribute> GetActiveCategories()
        {
            return _allActiveMarketCategories;
        }

        /// <summary>
        /// Retrieves the market region detail given a specific type.
        /// </summary>
        /// <param name="regionType">The type of market region</param>
        /// <returns>A market region detail</returns>
        public MarketRegionAttribute GetMarketRegion(MarketRegionType regionType)
        {
            return _marketRegionCache?.GetFilteredCache("Active")?[regionType] ?? throw new KeyNotFoundException($"Market region {regionType} not found in cache");
        }

        /// <summary>
        /// Determines which market category an item should be placed in.
        /// If category cannot be determined, MarketCategoryType.Miscellaneous will be returned.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>A market category type to place the item in.</returns>
        public MarketCategoryType GetItemMarketCategory(uint item)
        {
            var baseItemType = GetBaseItemType(item);
            var tag = GetTag(item);

            // Weapon Classes
            if (_itemService.VibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Vibroblade;
            if (_itemService.FinesseVibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.FinesseVibroblade;
            if (_itemService.HeavyVibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.HeavyVibroblade;
            if (_itemService.PolearmBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Polearm;
            if (_itemService.StaffBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Staff;
            if (_itemService.PistolBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Pistol;
            if (_itemService.ThrowingWeaponBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Throwing;
            if (_itemService.RifleBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Rifle;
            if (_itemService.TwinBladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.TwinBlade;
            if (_itemService.KatarBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Katar;
            if (_itemService.LightsaberBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Lightsaber;
            if (_itemService.SaberstaffBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Saberstaff;

            // Universal armor classes
            switch (baseItemType)
            {
                // Universal Armor
                case BaseItem.LargeShield:
                case BaseItem.SmallShield:
                case BaseItem.TowerShield:
                    return MarketCategoryType.Shield;
                case BaseItem.Cloak:
                    return MarketCategoryType.Cloak;
                case BaseItem.Belt:
                    return MarketCategoryType.Belt;
                case BaseItem.Ring:
                    return MarketCategoryType.Ring;
                case BaseItem.Amulet:
                    return MarketCategoryType.Necklace;
            }

            // Armor classes
            var armorType = _itemService.GetArmorType(item);
            if (armorType == ArmorType.Heavy)
            {
                switch (baseItemType)
                {
                    case BaseItem.Helmet:
                        return MarketCategoryType.Helmet;
                    case BaseItem.Gloves:
                    case BaseItem.Bracer:
                        return MarketCategoryType.Bracer;
                    case BaseItem.Boots:
                        return MarketCategoryType.Legging;
                    case BaseItem.Armor:
                        return MarketCategoryType.Breastplate;
                }
            }
            else if (armorType == ArmorType.Light)
            {
                switch (baseItemType)
                {
                    case BaseItem.Helmet:
                        return MarketCategoryType.Cap;
                    case BaseItem.Gloves:
                    case BaseItem.Bracer:
                        return MarketCategoryType.Glove;
                    case BaseItem.Boots:
                        return MarketCategoryType.Boot;
                    case BaseItem.Armor:
                        return MarketCategoryType.Tunic;
                }
            }

            // Recipes
            if (_craftService.IsItemRecipe(item))
                return MarketCategoryType.Recipe;
            // Components
            if (_craftService.IsItemComponent(item))
                return MarketCategoryType.Components;
            // Enhancements
            if (_craftService.IsItemEnhancement(item))
                return MarketCategoryType.Enhancement;

            // Ship Deeds
            if (_spaceService.IsItemShip(item))
                return MarketCategoryType.Starship;
            if (_spaceService.IsItemShipModule(item))
                return MarketCategoryType.StarshipParts;

            // Structures
            if (_propertyService.GetStructureTypeFromItem(item) != StructureType.Invalid)
                return MarketCategoryType.Structure;

            // Food
            if (tag == "FOOD")
                return MarketCategoryType.Food;

            // Pet Food
            if (tag == "PET_FOOD")
                return MarketCategoryType.PetFood;

            // Fishing Rods & Bait
            if (_fishingService.IsItemFishingRod(item) || _fishingService.IsItemBait(item))
                return MarketCategoryType.Fishing;

            // Incubation
            if (_beastMasteryService.IsIncubationCraftingItem(item))
                return MarketCategoryType.Incubation;

            // Beast Egg
            if (_beastMasteryService.IsBeastEgg(item))
                return MarketCategoryType.BeastEgg;

            // Blueprint
            if (_craftService.GetBlueprintDetails(item).Recipe != RecipeType.Invalid)
                return MarketCategoryType.Blueprint;

            //Starship Ammo
            if (_spaceService.IsStarshipAmmo(item))
                return MarketCategoryType.StarshipAmmo;
            
            return MarketCategoryType.Miscellaneous;
        }
    }
}
