using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Beasts.Entities;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Perk.Service
{
    public class Perk : IPerkService
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IGenericCacheService _cacheService;
        private readonly IBeastMasteryService _beastMasteryService;
        private readonly IPerkBuilder _perkBuilder;
        
        // Cached data
        private IEnumCache<PerkCategoryType, PerkCategoryAttribute> _categoryCache;
        private IInterfaceCache<PerkType, PerkDetail> _perkCache;
        private IEnumCache<CharacterType, CharacterTypeAttribute> _characterTypeCache;
        
        // Additional caches for complex data
        private readonly Dictionary<PerkCategoryType, List<PerkType>> _allPerksByCategory = new();
        private readonly Dictionary<PerkType, List<PerkTriggerEquippedAction>> _equipTriggers = new();
        private readonly Dictionary<PerkType, List<PerkTriggerUnequippedAction>> _unequipTriggers = new();
        private readonly Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> _purchaseTriggers = new();
        private readonly Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> _refundTriggers = new();
        private readonly Dictionary<PerkType, PerkDetail> _perksWithUnlockRequirements = new();
        private readonly Dictionary<PerkType, int> _perkMaxLevels = new();
        private readonly Dictionary<PerkType, Dictionary<int, int>> _perkLevelTiers = new();
        private readonly Dictionary<SkillType, List<PerkType>> _perksWithSkillRequirement = new();

        public Perk(ILogger logger, IDatabaseService db, IGenericCacheService cacheService, IBeastMasteryService beastMasteryService, IPerkBuilder perkBuilder)
        {
            _logger = logger;
            _db = db;
            _cacheService = cacheService;
            _beastMasteryService = beastMasteryService;
            _perkBuilder = perkBuilder;
        }

        /// <summary>
        /// Gets the list of heavy armor perks
        /// </summary>
        public List<PerkType> HeavyArmorPerks { get; } = new();

        /// <summary>
        /// Gets the list of light armor perks
        /// </summary>
        public List<PerkType> LightArmorPerks { get; } = new();
        
        // Pre-computed caches for fast retrieval
        private readonly Dictionary<PerkGroupType, Dictionary<PerkCategoryType, PerkCategoryAttribute>> _activeCategoriesByGroup = new();
        private readonly Dictionary<PerkGroupType, Dictionary<PerkType, PerkDetail>> _activePerksByGroup = new();
        private readonly Dictionary<PerkGroupType, Dictionary<PerkCategoryType, Dictionary<PerkType, PerkDetail>>> _activePerksByGroupAndCategory = new();
        private readonly Dictionary<PerkType, PerkDetail> _allPerks = new();
        private readonly Dictionary<PerkCategoryType, PerkCategoryAttribute> _allPerkCategories = new();
        private readonly Dictionary<CharacterType, CharacterTypeAttribute> _allCharacterTypes = new();

        /// <summary>
        /// When the module loads, cache all perk and character type information.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            CachePerks();
            CacheCharacterTypes();
            
            PopulatePreComputedCaches();
        }

        /// <summary>
        /// Caches perk information into various dictionaries for quicker look-ups later.
        /// </summary>
        private void CachePerks()
        {
            Console.WriteLine("PerkService.CachePerks() called - starting perk cache initialization...");
            
            // Cache perk categories
            _categoryCache = _cacheService.BuildEnumCache<PerkCategoryType, PerkCategoryAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", c => c.IsActive)
                .Build();

            // Initialize category lists
            foreach (var category in _categoryCache.AllItems.Keys)
            {
                _allPerksByCategory[category] = new List<PerkType>();
            }

            // Cache perks using interface discovery
            _perkCache = _cacheService.BuildInterfaceCache<IPerkListDefinition, PerkType, PerkDetail>()
                .WithDataExtractor(instance => instance.BuildPerks(_perkBuilder))
                .WithFilteredCache("Active", p => p.IsActive)
                .WithGroupedCache<PerkGroupType>("ByGroup", p => p.GroupType)
                .WithFilteredGroupedCache<PerkGroupType>("ActiveByGroup", p => p.IsActive, p => p.GroupType)
                .WithGroupedCache<PerkCategoryType>("ByCategory", p => p.Category)
                .Build();

            // Process all perks for additional caches
            foreach (var (perkType, perkDetail) in _perkCache.AllItems)
            {
                var categoryDetail = _categoryCache.AllItems[perkDetail.Category];

                // Add to active cache if the perk is active
                if (perkDetail.IsActive)
                {
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

                foreach (var (level, perkLevel) in perkDetail.PerkLevels)
                {
                    // If the perk has an "unlock requirement", add it to that cache.
                    var reqExists = perkLevel.Requirements.Count(x => x.GetType() == typeof(PerkRequirementUnlock)) > 0;
                    if (reqExists)
                    {
                        _perksWithUnlockRequirements[perkType] = perkDetail;
                        break;
                    }

                    var skillReqs = perkLevel
                        .Requirements.Where(x => x.GetType() == typeof(PerkRequirementSkill))
                        .Cast<PerkRequirementSkill>();

                    // Determine the tiers of each individual perk level.
                    // Also track the skill types used by this perk for later retrieval by the skill/perk decay system.
                    var highestRank = 0;
                    foreach (var req in skillReqs)
                    {
                        if (req.RequiredRank > highestRank)
                        {
                            highestRank = req.RequiredRank;
                        }

                        if (!_perksWithSkillRequirement.ContainsKey(req.Type))
                        {
                            _perksWithSkillRequirement[req.Type] = new List<PerkType>();
                        }

                        if (!_perksWithSkillRequirement[req.Type].Contains(perkType))
                        {
                            _perksWithSkillRequirement[req.Type].Add(perkType);
                        }
                    }
                    
                    var tier = highestRank / 10 + 1;
                    if (tier < 1)
                        tier = 1;
                    else if (tier > 5)
                        tier = 5;

                    if (!_perkLevelTiers.ContainsKey(perkType))
                        _perkLevelTiers[perkType] = new Dictionary<int, int>();

                    _perkLevelTiers[perkType][level] = tier;
                }

                // Add to the perks by category cache.
                _allPerksByCategory[perkDetail.Category].Add(perkType);

                // Determine the max level for the perk.
                _perkMaxLevels[perkType] = perkDetail.PerkLevels.Last().Key;
            }

            Console.WriteLine($"Loaded {_perkCache.AllItems.Count} player perks.");
        }

        /// <summary>
        /// Populates pre-computed caches for fast retrieval without LINQ.
        /// </summary>
        private void PopulatePreComputedCaches()
        {
            // Initialize group dictionaries
            foreach (PerkGroupType groupType in Enum.GetValues<PerkGroupType>())
            {
                if (groupType == PerkGroupType.Invalid) continue;
                
                _activeCategoriesByGroup[groupType] = new Dictionary<PerkCategoryType, PerkCategoryAttribute>();
                _activePerksByGroup[groupType] = new Dictionary<PerkType, PerkDetail>();
                _activePerksByGroupAndCategory[groupType] = new Dictionary<PerkCategoryType, Dictionary<PerkType, PerkDetail>>();
            }

            // Populate all perks cache
            foreach (var (perkType, perkDetail) in _perkCache!.AllItems)
            {
                _allPerks[perkType] = perkDetail;
            }

            // Populate all categories cache
            foreach (var (categoryType, categoryAttribute) in _categoryCache!.AllItems)
            {
                _allPerkCategories[categoryType] = categoryAttribute;
            }

            // Populate all character types cache
            foreach (var (characterType, characterTypeAttribute) in _characterTypeCache.AllItems)
            {
                _allCharacterTypes[characterType] = characterTypeAttribute;
            }

            // Populate caches by iterating through all perks once
            foreach (var (perkType, perkDetail) in _perkCache!.AllItems)
            {
                if (!perkDetail.IsActive) continue;

                var groupType = perkDetail.GroupType;
                if (groupType == PerkGroupType.Invalid) continue;

                // Add to active perks by group
                _activePerksByGroup[groupType][perkType] = perkDetail;

                // Add to active categories by group (if not already added)
                if (!_activeCategoriesByGroup[groupType].ContainsKey(perkDetail.Category))
                {
                    _activeCategoriesByGroup[groupType][perkDetail.Category] = _categoryCache!.AllItems[perkDetail.Category];
                }

                // Add to active perks by group and category
                if (!_activePerksByGroupAndCategory[groupType].ContainsKey(perkDetail.Category))
                {
                    _activePerksByGroupAndCategory[groupType][perkDetail.Category] = new Dictionary<PerkType, PerkDetail>();
                }
                _activePerksByGroupAndCategory[groupType][perkDetail.Category][perkType] = perkDetail;
            }
        }

        /// <summary>
        /// Caches character type information.
        /// </summary>
        private void CacheCharacterTypes()
        {
            _characterTypeCache = _cacheService.BuildEnumCache<CharacterType, CharacterTypeAttribute>()
                .WithAllItems()
                .Build();

            Console.WriteLine($"Loaded {_characterTypeCache.AllItems.Count} character types.");
        }

        /// <summary>
        /// Handles organizing triggers so future activation is quicker.
        /// </summary>
        /// <param name="perk">The perk to cache triggers for.</param>
        private void CacheTriggers(PerkDetail perk)
        {
            // Equipped Triggers: Fires when an item is equipped.
            if (perk.EquippedTriggers.Count > 0)
            {
                if (!_equipTriggers.ContainsKey(perk.Type))
                    _equipTriggers[perk.Type] = new List<PerkTriggerEquippedAction>();

                _equipTriggers[perk.Type].AddRange(perk.EquippedTriggers);
            }

            // Unequipped Triggers: Fires when an item is unequipped.
            if (perk.UnequippedTriggers.Count > 0)
            {
                if (!_unequipTriggers.ContainsKey(perk.Type))
                    _unequipTriggers[perk.Type] = new List<PerkTriggerUnequippedAction>();

                _unequipTriggers[perk.Type].AddRange(perk.UnequippedTriggers);
            }

            // Purchased Triggers: Fires when a perk is purchased.
            if (perk.PurchasedTriggers.Count > 0)
            {
                if (!_purchaseTriggers.ContainsKey(perk.Type))
                    _purchaseTriggers[perk.Type] = new List<PerkTriggerPurchasedRefundedAction>();

                _purchaseTriggers[perk.Type].AddRange(perk.PurchasedTriggers);
            }

            // Refunded Triggers: Fires when a perk is refunded.
            if (perk.PurchasedTriggers.Count > 0)
            {
                if (!_refundTriggers.ContainsKey(perk.Type))
                    _refundTriggers[perk.Type] = new List<PerkTriggerPurchasedRefundedAction>();

                _refundTriggers[perk.Type].AddRange(perk.RefundedTriggers);
            }
        }

        /// <summary>
        /// Retrieves all of the equip triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public Dictionary<PerkType, List<PerkTriggerEquippedAction>> GetAllEquipTriggers()
        {
            return _equipTriggers;
        }

        /// <summary>
        /// Retrieves all of the unequip triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public Dictionary<PerkType, List<PerkTriggerUnequippedAction>> GetAllUnequipTriggers()
        {
            return _unequipTriggers;
        }

        /// <summary>
        /// Retrieves all of the purchase triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllPurchaseTriggers()
        {
            return _purchaseTriggers;
        }

        /// <summary>
        /// Retrieves all of the refund triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllRefundTriggers()
        {
            return _refundTriggers;
        }


        /// <summary>
        /// Retrieves a list of all perks, including inactive ones.
        /// </summary>
        /// <returns>A list of all perks.</returns>
        public Dictionary<PerkType, PerkDetail> GetAllPerks()
        {
            return _perkCache?.AllItems.ToDictionary(x => x.Key, y => y.Value) ?? new Dictionary<PerkType, PerkDetail>();
        }

        /// <summary>
        /// Retrieves a list of all active perks, excluding inactive ones, by group.
        /// </summary>
        /// <returns>A list of all active perks.</returns>
        public Dictionary<PerkType, PerkDetail> GetAllActivePerks(PerkGroupType group)
        {
            return _activePerksByGroup.GetValueOrDefault(group) ?? new Dictionary<PerkType, PerkDetail>();
        }

        /// <summary>
        /// Retrieves a list of all perk categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all perk categories.</returns>
        public Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllPerkCategories()
        {
            return _categoryCache?.AllItems.ToDictionary(x => x.Key, y => y.Value) ?? new Dictionary<PerkCategoryType, PerkCategoryAttribute>();
        }

        /// <summary>
        /// Retrieves a list of all active perk categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of all active perk categories.</returns>
        public Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories()
        {
            var activeCategories = _categoryCache?.GetFilteredCache("Active");
            return activeCategories ?? new Dictionary<PerkCategoryType, PerkCategoryAttribute>();
        }

        /// <summary>
        /// Retrieves a list of all active perk categories for a specific group, excluding inactive ones.
        /// </summary>
        /// <param name="group">The group to filter by.</param>
        /// <returns>A list of all active perk categories for the specified group.</returns>
        public Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories(PerkGroupType group)
        {
            return _activeCategoriesByGroup.GetValueOrDefault(group) ?? new Dictionary<PerkCategoryType, PerkCategoryAttribute>();
        }

        /// <summary>
        /// Retrieves a list of all active perks by the specified category.
        /// </summary>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of all active perks in the specified category.</returns>
        public Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkCategoryType category)
        {
            var activeByCategory = _perkCache?.GetGroupedCache<PerkCategoryType>("ByCategory");
            var categoryPerks = activeByCategory?.GetValueOrDefault(category);
            return categoryPerks?.Where(x => x.Value.IsActive).ToDictionary(x => x.Key, y => y.Value) ?? new Dictionary<PerkType, PerkDetail>();
        }

        /// <summary>
        /// Retrieves a list of all active perks by the specified category and group.
        /// </summary>
        /// <param name="group">The group to filter by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of all active perks in the specified category and group.</returns>
        public Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkGroupType group, PerkCategoryType category)
        {
            var groupPerks = _activePerksByGroupAndCategory.GetValueOrDefault(group);
            if (groupPerks == null) return new Dictionary<PerkType, PerkDetail>();
            
            return groupPerks.GetValueOrDefault(category) ?? new Dictionary<PerkType, PerkDetail>();
        }

        /// <summary>
        /// Retrieves details about an individual perk.
        /// </summary>
        /// <param name="perkType">The type of perk to retrieve.</param>
        /// <returns>An object containing a perk's details.</returns>
        public PerkDetail GetPerkDetails(PerkType perkType)
        {
            if (_allPerks.Count == 0)
            {
                Console.WriteLine($"ERROR: _allPerks is empty when trying to get {perkType}!");
                Console.WriteLine($"_perkCache is null: {_perkCache == null}");
                if (_perkCache != null)
                {
                    Console.WriteLine($"_perkCache.AllItems.Count: {_perkCache.AllItems.Count}");
                }
                
                // Try to populate the cache on-demand as a fallback
                Console.WriteLine("Attempting to populate perk cache on-demand...");
                try
                {
                    CachePerks();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to populate perk cache on-demand: {ex.Message}");
                }
            }
            
            return _allPerks.TryGetValue(perkType, out var perk) 
                ? perk 
                : throw new KeyNotFoundException($"Perk {perkType} not found in cache");
        }

        /// <summary>
        /// Retrieves details about an individual perk category.
        /// </summary>
        /// <param name="categoryType">The type of category to retrieve.</param>
        /// <returns>An object containing a perk category's details.</returns>
        public PerkCategoryAttribute GetPerkCategoryDetails(PerkCategoryType categoryType)
        {
            return _allPerkCategories.TryGetValue(categoryType, out var category) 
                ? category 
                : throw new KeyNotFoundException($"Perk category {categoryType} not found in cache");
        }

        /// <summary>
        /// Retrieves the detail about a specific character type.
        /// </summary>
        /// <param name="characterType">The character type to retrieve.</param>
        /// <returns>A character type detail.</returns>
        public CharacterTypeAttribute GetCharacterType(CharacterType characterType)
        {
            return _allCharacterTypes.TryGetValue(characterType, out var characterTypeDetail) 
                ? characterTypeDetail 
                : throw new KeyNotFoundException($"Character type {characterType} not found in cache");
        }

        /// <summary>
        /// Retrieves the tier of a specific perk level.
        /// </summary>
        /// <param name="perkType">The type of perk</param>
        /// <param name="perkLevel">The level of the perk</param>
        /// <returns>The tier of the perk level. Returns 0 if unable to be determined.</returns>
        public int GetPerkLevelTier(PerkType perkType, int perkLevel)
        {
            if (!_perkLevelTiers.ContainsKey(perkType))
                return 0;
            if (!_perkLevelTiers[perkType].ContainsKey(perkLevel))
                return 0;

            return _perkLevelTiers[perkType][perkLevel];
        }

        /// <summary>
        /// Retrieves the perk level of a creature.
        /// On NPCs, this will retrieve the "PERK_LEVEL_{perkId}" variable, where {perkId} is replaced with the ID of the perk.
        /// If this variable is not set, the max level of the perk will be used instead.
        /// On PCs, this will retrieve the current perk level. It does not take into account any skill decay and should be
        /// treated as a "soft" check as requirements are assumed to have been checked prior.
        /// It is handled this way for performance reasons (checking requirements on perks is very expensive).
        /// If you need to perform a "hard" check on requirements, use GetEffectivePerkLevel instead.
        /// </summary>
        /// <param name="creature">The creature whose perk level will be retrieved.</param>
        /// <param name="perkType">The type of perk to retrieve.</param>
        /// <returns>The perk level of a creature.</returns>
        public int GetPerkLevel(uint creature, PerkType perkType)
        {
            if (GetIsDM(creature) && !GetIsDMPossessed(creature)) 
                return 0;

            // Players
            if (GetIsPC(creature) && !GetIsDMPossessed(creature))
            {
                return GetPlayerPerkLevel(creature, perkType);
            }
            // Beasts
            else if (_beastMasteryService.IsPlayerBeast(creature))
            {
                return GetBeastPerkLevel(creature, perkType);
            }
            // Creatures or DM-possessed creatures
            else
            {
                var perkLevel = GetLocalInt(creature, $"PERK_LEVEL_{(int) perkType}");
                var perkMaxLevel = perkType == PerkType.Invalid
                    ? 1
                    : _perkMaxLevels[perkType];
                return perkLevel > 0 ? perkLevel : perkMaxLevel;
            }
        }

        private int GetPlayerPerkLevel(uint player, PerkType perkType)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer == null)
                return 0;
            if (!dbPlayer.Perks.ContainsKey(perkType))
                return 0;

            return dbPlayer.Perks[perkType];
        }

        /// <summary>
        /// Retrieves a player's effective perk level.
        /// This performs a "hard" check on all perk requirements. This process is VERY expensive so please use sparingly.
        /// It is almost always better to use GetPerkLevel instead of this method.
        /// </summary>
        /// <param name="player">The player whose perk level we're retrieving</param>
        /// <param name="perkType">The type of perk we're retrieving</param>
        /// <returns>The player's effective perk level.</returns>
        public int GetPlayerEffectivePerkLevel(uint player, PerkType perkType)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return 0;

            return GetPlayerEffectivePerkLevel(player, dbPlayer, perkType);
        }

        /// <summary>
        /// Retrieves a player's effective perk level.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="dbPlayer">The database entity</param>
        /// <param name="perkType">The type of perk</param>
        /// <returns>The effective level for a given player and perk</returns>
        private int GetPlayerEffectivePerkLevel(uint player, Player dbPlayer, PerkType perkType)
        {
            var playerPerkLevel = dbPlayer.Perks.ContainsKey(perkType) ? dbPlayer.Perks[perkType] : 0;

            // Early exit if player doesn't have the perk at all.
            if (playerPerkLevel <= 0) return 0;

            // Retrieve perk levels at or below player's perk level and then order them from highest level to lowest.
            var perk = GetPerkDetails(perkType);
            var perkLevels = perk.PerkLevels
                .Where(x => x.Key <= playerPerkLevel)
                .OrderByDescending(o => o.Key);

            // Iterate over each perk level and check requirements.
            // The first perk level the player passes requirements on is the player's effective level.
            foreach (var (level, detail) in perkLevels)
            {
                // No requirements set for this perk level. Return the level.
                if (detail.Requirements.Count <= 0) 
                    return level;

                var meetsRequirements = true;
                foreach (var req in detail.Requirements)
                {
                    if (!string.IsNullOrWhiteSpace(req.CheckRequirements(player)))
                    {
                        meetsRequirements = false;
                        break;
                    }
                }

                if (meetsRequirements)
                {
                    return level;
                }
            }

            // Otherwise none of the perk level requirements passed. Player's effective level is zero.
            return 0;
        }

        /// <summary>
        /// Retrieves a beast's effective perk level.
        /// </summary>
        /// <param name="beast"></param>
        /// <param name="perkType"></param>
        /// <returns></returns>
        private int GetBeastPerkLevel(uint beast, PerkType perkType)
        {

            // todo: merge with player branch
            var beastId = _beastMasteryService.GetBeastId(beast);
            var dbBeast = _db.Get<Beast>(beastId);

            if (dbBeast == null)
                return 0;

            var player = GetMaster(beast);
            if (!GetIsPC(player) || !GetIsObjectValid(player))
                return 0;

            var beastPerkLevel = dbBeast.Perks.ContainsKey(perkType) ? dbBeast.Perks[perkType] : 0;

            // Early exit if player doesn't have the perk at all.
            if (beastPerkLevel <= 0) return 0;

            // Retrieve perk levels at or below player's perk level and then order them from highest level to lowest.
            var perk = GetPerkDetails(perkType);
            var perkLevels = perk.PerkLevels
                .Where(x => x.Key <= beastPerkLevel)
                .OrderByDescending(o => o.Key);

            // Iterate over each perk level and check requirements.
            // The first perk level the player passes requirements on is the player's effective level.
            foreach (var (level, detail) in perkLevels)
            {
                // No requirements set for this perk level. Return the level.
                if (detail.Requirements.Count <= 0) 
                    return level;

                foreach (var req in detail.Requirements)
                {
                    if (string.IsNullOrWhiteSpace(req.CheckRequirements(player))) 
                        return level;
                }
            }


            return 0;
        }


        /// <summary>
        /// This will mark a perk as unlocked for a player.
        /// If the perk does not have an "unlock requirement", nothing will happen.
        /// This will do a DB call so be sure to refresh your entity instance after calling this.
        /// </summary>
        /// <param name="player">The player to unlock the perk for</param>
        /// <param name="perkType">The type of perk to unlock for the player</param>
        public void UnlockPerkForPlayer(uint player, PerkType perkType)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (!_perksWithUnlockRequirements.ContainsKey(perkType)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer.UnlockedPerks.ContainsKey(perkType)) return;

            dbPlayer.UnlockedPerks[perkType] = DateTime.UtcNow;
            _db.Set(dbPlayer);
        }

        /// <summary>
        /// When a skill receives decay, any perks tied to that skill should be checked.
        /// If the player no longer meets the requirements for those perks, they should be reduced in level.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorLoseSkill)]
        public void RemovePerkLevelOnSkillDecay()
        {
            var skillType = (SkillType)Convert.ToInt32(EventsPlugin.GetEventData("SKILL_TYPE_ID"));
            
            // Early exit - if no perks are tied to this skill, then it doesn't matter. There's nothing to remove.
            if (!_perksWithSkillRequirement.ContainsKey(skillType))
                return;

            var player = OBJECT_SELF;
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var possiblePerks = _perksWithSkillRequirement[skillType];
            
            foreach (var perkType in possiblePerks)
            {
                // Player doesn't have this perk. Move to the next.
                if (!dbPlayer.Perks.ContainsKey(perkType))
                    continue;

                var perkDetail = GetPerkDetails(perkType);
                var effectiveLevel = GetPlayerEffectivePerkLevel(player, perkType);
                var currentLevel = dbPlayer.Perks[perkType];

                // Player didn't suffer a reduction in effective level. Move to the next.
                if (effectiveLevel == currentLevel)
                    continue;

                // Found at least one perk level that needs to be removed.
                for (var level = currentLevel; level > effectiveLevel; level--)
                {
                    var perkLevel = perkDetail.PerkLevels[level];
                    dbPlayer.UnallocatedSP += perkLevel.Price;

                    foreach (var feat in perkLevel.GrantedFeats)
                    {
                        CreaturePlugin.RemoveFeat(player, feat);
                    }
                    
                    _logger.Write<PerkRefundLogGroup>($"AUTOMATIC DECAY REFUND - {playerId} - Refunded Date {DateTime.UtcNow} - Level {perkLevel} - PerkID {perkType}");
                    FloatingTextStringOnCreature($"Perk '{perkDetail.Name}' level {level} was refunded because your skill fell under the minimum requirements. You reclaimed {perkLevel.Price} SP.", player, false);
                }

                dbPlayer.Perks[perkType] = effectiveLevel;
                _db.Set(dbPlayer);

                foreach (var refundTrigger in perkDetail.RefundedTriggers)
                {
                    refundTrigger(player);
                }
            }

            ExportSingleCharacter(player);
        }
    }
}
