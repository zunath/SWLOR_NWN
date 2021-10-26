using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PlayerMarketService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
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
        /// Retrieves the icon used on the market UIs. 
        /// </summary>
        /// <param name="item">The item to retrieve the icon for.</param>
        /// <returns>A resref of the icon to use.</returns>
        public static string GetIconResref(uint item)
        {
            var baseItem = GetBaseItemType(item);

            if (baseItem == BaseItem.Cloak) // Cloaks use PLTs so their default icon doesn't really work
                return "iit_cloak";
            else if (baseItem == BaseItem.SpellScroll || baseItem == BaseItem.EnchantedScroll)
            {// Scrolls get their icon from the cast spell property
                if (GetItemHasItemProperty(item, ItemPropertyType.CastSpell))
                {
                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.CastSpell)
                            return Get2DAString("iprp_spells", "Icon", GetItemPropertySubType(ip));
                    }
                }
            }
            else if (Get2DAString("baseitems", "ModelType", (int)baseItem) == "0")
            {// Create the icon resref for simple modeltype items
                var sSimpleModelId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0).ToString();
                while (GetStringLength(sSimpleModelId) < 3)
                {
                    sSimpleModelId = "0" + sSimpleModelId;
                }

                var sDefaultIcon = Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
                switch (baseItem)
                {
                    case BaseItem.MiscSmall:
                    case BaseItem.CraftMaterialSmall:
                        sDefaultIcon = "iit_smlmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscMedium:
                    case BaseItem.CraftMaterialMedium:
                    case BaseItem.CraftBase:
                        sDefaultIcon = "iit_midmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscLarge:
                        sDefaultIcon = "iit_talmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscThin:
                        sDefaultIcon = "iit_thnmisc_" + sSimpleModelId;
                        break;
                }

                var nLength = GetStringLength(sDefaultIcon);
                if (GetSubString(sDefaultIcon, nLength - 4, 1) == "_")// Some items have a default icon of xx_yyy_001, we strip the last 4 symbols if that is the case
                    sDefaultIcon = GetStringLeft(sDefaultIcon, nLength - 4);
                var sIcon = sDefaultIcon + "_" + sSimpleModelId;
                if (ResManGetAliasFor(sIcon, ResType.TGA) != "")// Check if the icon actually exists, if not, we'll fall through and return the default icon
                    return sIcon;
            }

            // For everything else use the item's default icon
            return Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
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

            // Ship Deeds
            if (Space.IsItemShip(item))
                return MarketCategoryType.Starship;
            if (Space.IsItemShipModule(item))
                return MarketCategoryType.StarshipParts;

            return MarketCategoryType.Miscellaneous;
        }
    }
}
