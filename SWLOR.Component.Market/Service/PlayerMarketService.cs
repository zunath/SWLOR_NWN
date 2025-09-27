using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Market.Contracts;
using SWLOR.Component.Market.Enums;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Fishing.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Market.Enums;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using MarketCategoryType = SWLOR.Shared.Domain.Market.Enums.MarketCategoryType;

namespace SWLOR.Component.Market.Service
{
    public class PlayerMarketService: IPlayerMarketService
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        public const int MaxListingsPerMarket = 25;
        
        // Cached data
        private IEnumCache<MarketCategoryType, MarketCategoryAttribute> _marketCategoryCache;
        private IEnumCache<MarketRegionType, MarketRegionAttribute> _marketRegionCache;
        
        // Additional caches for backward compatibility
        private readonly Dictionary<MarketCategoryType, MarketCategoryAttribute> _activeMarketCategories = new();
        
        // Pre-computed cache for fast retrieval
        private readonly Dictionary<MarketCategoryType, MarketCategoryAttribute> _allActiveMarketCategories = new();

        public PlayerMarketService(
            IDatabaseService db, 
            IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IGenericCacheService CacheService => _serviceProvider.GetRequiredService<IGenericCacheService>();
        private ICraftService CraftService => _serviceProvider.GetRequiredService<ICraftService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IPropertyService PropertyService => _serviceProvider.GetRequiredService<IPropertyService>();
        private IFishingService FishingService => _serviceProvider.GetRequiredService<IFishingService>();
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private IBeastMasteryService BeastMasteryService => _serviceProvider.GetRequiredService<IBeastMasteryService>();


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
            _marketCategoryCache = CacheService.BuildEnumCache<MarketCategoryType, MarketCategoryAttribute>()
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
            _marketRegionCache = CacheService.BuildEnumCache<MarketRegionType, MarketRegionAttribute>()
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
            if (ItemService.VibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Vibroblade;
            if (ItemService.FinesseVibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.FinesseVibroblade;
            if (ItemService.HeavyVibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.HeavyVibroblade;
            if (ItemService.PolearmBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Polearm;
            if (ItemService.StaffBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Staff;
            if (ItemService.PistolBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Pistol;
            if (ItemService.ThrowingWeaponBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Throwing;
            if (ItemService.RifleBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Rifle;
            if (ItemService.TwinBladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.TwinBlade;
            if (ItemService.KatarBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Katar;
            if (ItemService.LightsaberBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Lightsaber;
            if (ItemService.SaberstaffBaseItemTypes.Contains(baseItemType))
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
            var armorType = ItemService.GetArmorType(item);
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
            if (CraftService.IsItemRecipe(item))
                return MarketCategoryType.Recipe;
            // Components
            if (CraftService.IsItemComponent(item))
                return MarketCategoryType.Components;
            // Enhancements
            if (CraftService.IsItemEnhancement(item))
                return MarketCategoryType.Enhancement;

            // Ship Deeds
            if (SpaceService.IsItemShip(item))
                return MarketCategoryType.Starship;
            if (SpaceService.IsItemShipModule(item))
                return MarketCategoryType.StarshipParts;

            // Structures
            if (PropertyService.GetStructureTypeFromItem(item) != StructureType.Invalid)
                return MarketCategoryType.Structure;

            // Food
            if (tag == "FOOD")
                return MarketCategoryType.Food;

            // Pet Food
            if (tag == "PET_FOOD")
                return MarketCategoryType.PetFood;

            // Fishing Rods & Bait
            if (FishingService.IsItemFishingRod(item) || FishingService.IsItemBait(item))
                return MarketCategoryType.Fishing;

            // Incubation
            if (BeastMasteryService.IsIncubationCraftingItem(item))
                return MarketCategoryType.Incubation;

            // Beast Egg
            if (BeastMasteryService.IsBeastEgg(item))
                return MarketCategoryType.BeastEgg;

            // Blueprint
            if (CraftService.GetBlueprintDetails(item).Recipe != RecipeType.Invalid)
                return MarketCategoryType.Blueprint;

            //Starship Ammo
            if (SpaceService.IsStarshipAmmo(item))
                return MarketCategoryType.StarshipAmmo;
            
            return MarketCategoryType.Miscellaneous;
        }
    }
}
