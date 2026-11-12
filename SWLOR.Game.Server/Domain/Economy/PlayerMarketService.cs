using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.NWN.API.NWScript.Enum.Item;
using MarketCategoryType = SWLOR.Game.Server.Domain.Economy.MarketCategoryType;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The player market: the logic and state that used to be a static class, moved here
    /// unchanged. The event handlers run on the container's instance; static callers reach it through <c>GameServices.PlayerMarket</c>.
    /// </summary>
    public class PlayerMarketService : IPlayerMarketService
    {
        /// <summary>
        /// A blueprint local int that keeps an item off the market.
        /// </summary>
        public const string NoMarketListingVariable = "NO_MARKET_LISTING";

        public const int MaxListingsPerMarket = 25;

        private readonly IDBService _db;
        private readonly IBeastMasteryService _beastMastery;
        private readonly ICraftService _craft;
        private readonly IFishingService _fishing;
        private readonly IItemService _item;
        private readonly IMusicService _music;
        private readonly IPropertyService _property;
        private Dictionary<MarketCategoryType, MarketCategoryAttribute> _activeMarketCategories = new Dictionary<MarketCategoryType, MarketCategoryAttribute>();
        private readonly Dictionary<MarketRegionType, MarketRegionAttribute> _activeMarketRegions = new Dictionary<MarketRegionType, MarketRegionAttribute>();

        private readonly ISpaceService _space;

        public PlayerMarketService(
            IDBService db,
            IBeastMasteryService beastMastery,
            ICraftService craft,
            IFishingService fishing,
            IItemService item,
            IMusicService music,
            IPropertyService property,
            ISpaceService space)
        {
            _db = db;
            _beastMastery = beastMastery;
            _craft = craft;
            _fishing = fishing;
            _item = item;
            _music = music;
            _property = property;
            _space = space;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            LoadMarketCategories();
            LoadMarkets();
        }

        [BootPhase(BootReload.BootOnly, NothingToAdopt = "It deletes expired listings in Redis; the next boot does it again.")]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
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

        [PlayerSession(PlayerReload.LoginOnly,
            "A login message.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
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
            if (_item.VibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Vibroblade;
            if (_item.VibroknifeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Vibroknife;
            if (_item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.HeavyVibroblade;
            if (_item.SpearBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Spear;
            if (_item.StaffBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Staff;
            if (_item.PistolBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Pistol;
            if (_item.ThrowingWeaponBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Throwing;
            if (_item.RifleBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Rifle;
            if (_item.TwinBladeBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.TwinBlade;
            if (_item.KatarBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Katar;
            if (_item.PowerGloveBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.PowerGlove;
            if (_item.LightsaberBaseItemTypes.Contains(baseItemType))
                return MarketCategoryType.Lightsaber;
            if (_item.SaberstaffBaseItemTypes.Contains(baseItemType))
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
            if (_craft.IsItemRecipe(item))
                return MarketCategoryType.Recipe;
            // Components
            if (_craft.IsItemComponent(item))
                return MarketCategoryType.Components;
            // Enhancements
            if (_craft.IsItemEnhancement(item))
                return MarketCategoryType.Enhancement;

            // Ship Deeds
            if (_space.IsItemShip(item))
                return MarketCategoryType.Starship;
            if (_space.IsItemShipModule(item))
                return MarketCategoryType.StarshipParts;

            // Structures
            if (_property.GetStructureTypeFromItem(item) != StructureType.Invalid)
                return MarketCategoryType.Structure;

            // Food
            if (tag == "FOOD")
                return MarketCategoryType.Food;

            // Pet Food
            if (tag == "PET_FOOD")
                return MarketCategoryType.PetFood;

            // Fishing Rods & Bait
            if (_fishing.IsItemFishingRod(item) || _fishing.IsItemBait(item))
                return MarketCategoryType.Fishing;

            // Incubation
            if (_beastMastery.IsIncubationCraftingItem(item))
                return MarketCategoryType.Incubation;

            // Beast Egg
            if (_beastMastery.IsBeastEgg(item))
                return MarketCategoryType.BeastEgg;

            // Blueprint
            if (_craft.GetBlueprintDetails(item).Recipe != RecipeType.Invalid)
                return MarketCategoryType.Blueprint;

            //Starship Ammo
            if (_space.IsStarshipAmmo(item))
                return MarketCategoryType.StarshipAmmo;

            // Song discs and music players
            if (_music.IsMusicItem(item))
                return MarketCategoryType.Music;

            return MarketCategoryType.Miscellaneous;
        }
    }
}
