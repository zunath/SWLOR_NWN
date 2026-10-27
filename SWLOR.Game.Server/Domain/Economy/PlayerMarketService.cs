using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using MarketCategoryType = SWLOR.Game.Server.Service.PlayerMarketService.MarketCategoryType;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The player market: the logic and state that used to be static in <c>Service.PlayerMarket</c>, moved here
    /// unchanged. The facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class PlayerMarketService : IPlayerMarketService
    {
        private readonly IDBService _db;
        private Dictionary<MarketCategoryType, MarketCategoryAttribute> _activeMarketCategories = new Dictionary<MarketCategoryType, MarketCategoryAttribute>();
        private readonly Dictionary<MarketRegionType, MarketRegionAttribute> _activeMarketRegions = new Dictionary<MarketRegionType, MarketRegionAttribute>();

        public PlayerMarketService(IDBService db)
        {
            _db = db;
        }

        public void CacheData()
        {
            LoadMarketCategories();
            LoadMarkets();
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
        private void LoadMarketCategories()
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
        private void LoadMarkets()
        {
            var categories = Enum.GetValues(typeof(MarketRegionType)).Cast<MarketRegionType>();
            foreach (var category in categories)
            {
                var attribute = category.GetAttribute<MarketRegionType, MarketRegionAttribute>();

                if (attribute.IsActive)
                    _activeMarketRegions[category] = attribute;
            }
        }

        public Dictionary<MarketCategoryType, MarketCategoryAttribute> GetActiveCategories()
        {
            return _activeMarketCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        public MarketRegionAttribute GetMarketRegion(MarketRegionType regionType)
        {
            return _activeMarketRegions[regionType];
        }

        public MarketCategoryType? GetBaseItemMarketCategory(BaseItem baseItemType)
        {
            // Weapon Classes
            if (Item.VibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Vibroblade;
            if (Item.VibroknifeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Vibroknife;
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.HeavyVibroblade;
            if (Item.SpearBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Spear;
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
            if (Item.PowerGloveBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.PowerGlove;
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
            switch (baseItemType)
            {
                case BaseItem.Helmet:
                    return MarketCategoryType.Helmet;
                case BaseItem.Gloves:
                case BaseItem.Bracer:
                    return MarketCategoryType.Glove;
                case BaseItem.Boots:
                    return MarketCategoryType.Boot;
                case BaseItem.Armor:
                    return MarketCategoryType.Armor;
            }

            return null;
        }

        public MarketCategoryType GetItemMarketCategory(uint item)
        {
            var baseItemType = GetBaseItemType(item);
            var tag = GetTag(item);

            var baseItemCategory = GetBaseItemMarketCategory(baseItemType);
            if (baseItemCategory != null)
                return baseItemCategory.Value;

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
            if (Service.Space.IsItemShip(item))
                return MarketCategoryType.Starship;
            if (Service.Space.IsItemShipModule(item))
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

            // Incubation
            if (BeastMastery.IsIncubationCraftingItem(item))
                return MarketCategoryType.Incubation;

            // Beast Egg
            if (BeastMastery.IsBeastEgg(item))
                return MarketCategoryType.BeastEgg;

            // Blueprint
            if (Craft.GetBlueprintDetails(item).Recipe != RecipeType.Invalid)
                return MarketCategoryType.Blueprint;

            //Starship Ammo
            if (Service.Space.IsStarshipAmmo(item))
                return MarketCategoryType.StarshipAmmo;

            // Song discs and music players
            if (Music.IsMusicItem(item))
                return MarketCategoryType.Music;

            return MarketCategoryType.Miscellaneous;
        }
    }
}
