using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service
{
    public static partial class Perk
    {
        // All categories, including inactive
        private static readonly Dictionary<PerkCategoryType, PerkCategoryAttribute> _allCategories = new();

        // Active categories only
        private static readonly Dictionary<PerkCategoryType, PerkCategoryAttribute> _activeCategories = new();

        // All perks, including inactive
        private static readonly Dictionary<PerkType, PerkDetail> _allPerks = new();
        private static readonly Dictionary<PerkCategoryType, List<PerkType>> _allPerksByCategory = new();

        // Active perks only
        private static readonly Dictionary<PerkType, PerkDetail> _activePerks = new();
        private static readonly Dictionary<PerkCategoryType, Dictionary<PerkType, PerkDetail>> _activePerksByCategory = new();

        // Trigger Actions
        private static readonly Dictionary<PerkType, List<PerkTriggerEquippedAction>> _equipTriggers = new();
        private static readonly Dictionary<PerkType, List<PerkTriggerUnequippedAction>> _unequipTriggers = new();
        private static readonly Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> _purchaseTriggers = new();
        private static readonly Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> _refundTriggers = new();

        // Perks with unlock requirements
        private static readonly Dictionary<PerkType, PerkDetail> _perksWithUnlockRequirements = new();

        private static readonly Dictionary<PerkType, int> _perkMaxLevels = new();

        private static readonly Dictionary<CharacterType, CharacterTypeAttribute> _characterTypes = new();

        /// <summary>
        /// Gets the list of heavy armor perks
        /// </summary>
        public static List<PerkType> HeavyArmorPerks { get; } = new();

        /// <summary>
        /// Gets the list of light armor perks
        /// </summary>
        public static List<PerkType> LightArmorPerks { get; } = new();

        /// <summary>
        /// When the module loads, cache all perk and character type information.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CachePerks();
            CacheCharacterTypes();
        }

        /// <summary>
        /// Caches perk information into various dictionaries for quicker look-ups later.
        /// </summary>
        private static void CachePerks()
        {
            var categories = Enum.GetValues(typeof(PerkCategoryType)).Cast<PerkCategoryType>();
            foreach (var category in categories)
            {
                var categoryDetail = category.GetAttribute<PerkCategoryType, PerkCategoryAttribute>();
                _allCategories[category] = categoryDetail;
                _allPerksByCategory[category] = new List<PerkType>();

                if (categoryDetail.IsActive)
                {
                    _activePerksByCategory[category] = new Dictionary<PerkType, PerkDetail>();
                }
            }

            // Organize perks to make later reads quicker.
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IPerkListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IPerkListDefinition) Activator.CreateInstance(type);
                var perks = instance.BuildPerks();

                foreach (var (perkType, perkDetail) in perks)
                {
                    var categoryDetail = _allCategories[perkDetail.Category];

                    // Add to the perks cache
                    _allPerks[perkType] = perkDetail;

                    // Add to active cache if the perk is active
                    if (perkDetail.IsActive)
                    {
                        _activePerks[perkType] = perkDetail;

                        if (!_activePerksByCategory.ContainsKey(perkDetail.Category))
                            _activePerksByCategory[perkDetail.Category] = new Dictionary<PerkType, PerkDetail>();

                        _activePerksByCategory[perkDetail.Category][perkType] = perkDetail;

                        if (perkDetail.Category == PerkCategoryType.ArmorHeavy)
                        {
                            HeavyArmorPerks.Add(perkType);
                        }
                        else if (perkDetail.Category == PerkCategoryType.ArmorLight)
                        {
                            LightArmorPerks.Add(perkType);
                        }

                        // Add appropriate trigger entries if this perk is active and has them.
                        CacheTriggers(perkDetail);
                    }

                    // Add to active category cache if the perk and category are both active.
                    if (perkDetail.IsActive && categoryDetail.IsActive)
                    {
                        _activeCategories[perkDetail.Category] = categoryDetail;
                    }

                    // If the perk has an "unlock requirement", add it to that cache.
                    foreach (var level in perkDetail.PerkLevels)
                    {
                        var reqExists = level.Value.Requirements.Count(x => x.GetType() == typeof(PerkRequirementUnlock)) > 0;
                        if (reqExists)
                        {
                            _perksWithUnlockRequirements[perkType] = perkDetail;
                            break;
                        }
                    }

                    // Add to the perks by category cache.
                    _allPerksByCategory[perkDetail.Category].Add(perkType);

                    // Determine the max level for the perk.
                    _perkMaxLevels[perkType] = perkDetail.PerkLevels.Last().Key;
                    Console.WriteLine($"Loaded {perkDetail.Category}: {perkDetail.Name}.");
                }
            }

            Console.WriteLine($"Loaded {_allPerks.Count} perks.");
        }

        /// <summary>
        /// Caches character type information.
        /// </summary>
        private static void CacheCharacterTypes()
        {
            var categories = Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>();
            foreach (var type in categories)
            {
                var characterTypeDetail = type.GetAttribute<CharacterType, CharacterTypeAttribute>();
                _characterTypes[type] = characterTypeDetail;
            }

            Console.WriteLine($"Loaded {_characterTypes.Count} character types.");
        }

        /// <summary>
        /// Handles organizing triggers so future activation is quicker.
        /// </summary>
        /// <param name="perk">The perk to cache triggers for.</param>
        private static void CacheTriggers(PerkDetail perk)
        {
            // Equipped Triggers: Fires when an item is equipped.
            if (perk.EquippedTriggers.Count > 0)
            {
                if(!_equipTriggers.ContainsKey(perk.Type))
                    _equipTriggers[perk.Type] = new List<PerkTriggerEquippedAction>();

                _equipTriggers[perk.Type].AddRange(perk.EquippedTriggers);
            }

            // Unequipped Triggers: Fires when an item is unequipped.
            if (perk.UnequippedTriggers.Count > 0)
            {
                if(!_unequipTriggers.ContainsKey(perk.Type))
                    _unequipTriggers[perk.Type] = new List<PerkTriggerUnequippedAction>();

                _unequipTriggers[perk.Type].AddRange(perk.UnequippedTriggers);
            }

            // Purchased Triggers: Fires when a perk is purchased.
            if (perk.PurchasedTriggers.Count > 0)
            {
                if(!_purchaseTriggers.ContainsKey(perk.Type))
                    _purchaseTriggers[perk.Type] = new List<PerkTriggerPurchasedRefundedAction>();

                _purchaseTriggers[perk.Type].AddRange(perk.PurchasedTriggers);
            }

            // Refunded Triggers: Fires when a perk is refunded.
            if (perk.PurchasedTriggers.Count > 0)
            {
                if(!_refundTriggers.ContainsKey(perk.Type))
                    _refundTriggers[perk.Type] = new List<PerkTriggerPurchasedRefundedAction>();

                _refundTriggers[perk.Type].AddRange(perk.RefundedTriggers);
            }
        }

        /// <summary>
        /// Retrieves all of the equip triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PerkType, List<PerkTriggerEquippedAction>> GetAllEquipTriggers()
        {
            return _equipTriggers.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves all of the unequip triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PerkType, List<PerkTriggerUnequippedAction>> GetAllUnequipTriggers()
        {
            return _unequipTriggers.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves all of the purchase triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllPurchaseTriggers()
        {
            return _purchaseTriggers.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves all of the refund triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllRefundTriggers()
        {
            return _refundTriggers.ToDictionary(x => x.Key, y => y.Value);
        }


        /// <summary>
        /// Retrieves a list of all perks, including inactive ones.
        /// </summary>
        /// <returns>A list of all perks.</returns>
        public static Dictionary<PerkType, PerkDetail> GetAllPerks()
        {
            return _allPerks.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all active perks, excluding inactive ones.
        /// </summary>
        /// <returns>A list of all active perks.</returns>
        public static Dictionary<PerkType, PerkDetail> GetAllActivePerks()
        {
            return _activePerks.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all perk categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all perk categories.</returns>
        public static Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllPerkCategories()
        {
            return _allCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all active perk categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of all active perk categories.</returns>
        public static Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories()
        {
            return _activeCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all active perks by the specified category.
        /// </summary>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of all active perks in the specified category.</returns>
        public static Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkCategoryType category)
        {
            return _activePerksByCategory[category].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves details about an individual perk.
        /// </summary>
        /// <param name="perkType">The type of perk to retrieve.</param>
        /// <returns>An object containing a perk's details.</returns>
        public static PerkDetail GetPerkDetails(PerkType perkType)
        {
            return _allPerks[perkType];
        }

        /// <summary>
        /// Retrieves details about an individual perk category.
        /// </summary>
        /// <param name="categoryType">The type of category to retrieve.</param>
        /// <returns>An object containing a perk category's details.</returns>
        public static PerkCategoryAttribute GetPerkCategoryDetails(PerkCategoryType categoryType)
        {
            return _allCategories[categoryType];
        }

        /// <summary>
        /// Retrieves the detail about a specific character type.
        /// </summary>
        /// <param name="characterType">The character type to retrieve.</param>
        /// <returns>A character type detail.</returns>
        public static CharacterTypeAttribute GetCharacterType(CharacterType characterType)
        {
            return _characterTypes[characterType];
        }
    }
}
