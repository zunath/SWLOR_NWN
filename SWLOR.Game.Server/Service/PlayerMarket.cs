using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.PropertyService;
using MarketCategoryType = SWLOR.Game.Server.Service.PlayerMarketService.MarketCategoryType;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerMarket
    {
        public const int MaxListingsPerMarket = 25;
        private static Dictionary<MarketCategoryType, MarketCategoryAttribute> _activeMarketCategories = new();
        private static readonly Dictionary<MarketRegionType, MarketRegionAttribute> _activeMarketRegions = new();

        /// <summary>
        /// When the module caches, cache all static player market data for quick retrieval.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            LoadMarketCategories();
            LoadMarkets();
        }

        /// <summary>
        /// Marks items as unlisted if they have been sitting on the market for longer than two weeks.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void RemoveOldListings()
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true);
            var count = (int)DB.SearchCount(query);
            var listings = DB.Search(query
                .AddPaging(count, 0));
            var now = DateTime.UtcNow;

            foreach (var listing in listings)
            {
                if (listing.DateListed != null && Math.Abs(now.Subtract((DateTime)listing.DateListed).Days) >= 14)
                {
                    listing.IsListed = false;

                    DB.Set(listing);
                }
            }
        }

        /// <summary>
        /// When a player enters the server, if they have credits in their market till, send them a message stating so.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void CheckMarketTill()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

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
        private static void LoadMarketCategories()
        {
            var categories = Enum.GetValues(typeof(MarketCategoryType)).Cast<MarketCategoryType>();
            foreach (var category in categories)
            {
                var attribute = category.GetAttribute<MarketCategoryType, MarketCategoryAttribute>();

                if(attribute.IsActive)
                    _activeMarketCategories[category] = attribute;
            }

            _activeMarketCategories = _activeMarketCategories.OrderBy(o => o.Value.Name)
                .ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Reads all of the MarketRegionType enumerations and adds them to the related dictionaries.
        /// </summary>
        private static void LoadMarkets()
        {
            var categories = Enum.GetValues(typeof(MarketRegionType)).Cast<MarketRegionType>();
            foreach (var category in categories)
            {
                var attribute = category.GetAttribute<MarketRegionType, MarketRegionAttribute>();

                if (attribute.IsActive)
                    _activeMarketRegions[category] = attribute;
            }
        }

        /// <summary>
        /// Retrieves all active market categories.
        /// </summary>
        /// <returns>A dictionary of active market categories.</returns>
        public static Dictionary<MarketCategoryType, MarketCategoryAttribute> GetActiveCategories()
        {
            return _activeMarketCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves the market region detail given a specific type.
        /// </summary>
        /// <param name="regionType">The type of market region</param>
        /// <returns>A market region detail</returns>
        public static MarketRegionAttribute GetMarketRegion(MarketRegionType regionType)
        {
            return _activeMarketRegions[regionType];
        }

        /// <summary>
        /// Determines which market category an item should be placed in.
        /// If category cannot be determined, MarketCategoryType.Miscellaneous will be returned.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>A market category type to place the item in.</returns>
        public static MarketCategoryType GetItemMarketCategory(uint item)
        {
            var baseItemType = GetBaseItemType(item);
            var tag = GetTag(item);

            // Weapon Classes
            if (Item.VibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Vibroblade;
            if (Item.FinesseVibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.FinesseVibroblade;
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.HeavyVibroblade;
            if (Item.PolearmBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Polearm;
            if (Item.StaffBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Staff;
            if (Item.PistolBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Pistol;
            if (Item.ThrowingWeaponBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Throwing;
            if (Item.RifleBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Rifle;
            if (Item.TwinBladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.TwinBlade;
            if (Item.KatarBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Katar;
            if (Item.LightsaberBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Lightsaber;
            if (Item.SaberstaffBaseItemTypes.Contains(baseItemType))
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
            var armorType = Item.GetArmorType(item);
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
            if (Craft.IsItemRecipe(item))
                return MarketCategoryType.Recipe;
            // Components
            if (Craft.IsItemComponent(item))
                return MarketCategoryType.Components;
            // Enhancements
            if (Craft.IsItemEnhancement(item))
                return MarketCategoryType.Enhancement;

            // Ship Deeds
            if (Space.IsItemShip(item))
                return MarketCategoryType.Starship;
            if (Space.IsItemShipModule(item))
                return MarketCategoryType.StarshipParts;

            // Structures
            if (Property.GetStructureTypeFromItem(item) != StructureType.Invalid)
                return MarketCategoryType.Structure;

            // Food
            if (tag == "FOOD")
                return MarketCategoryType.Food;

            // Pet Food
            if (tag == "PET_FOOD")
                return MarketCategoryType.PetFood;

            // Fishing Rods & Bait
            if (Fishing.IsItemFishingRod(item) || Fishing.IsItemBait(item))
                return MarketCategoryType.Fishing;

            return MarketCategoryType.Miscellaneous;
        }
    }
}
