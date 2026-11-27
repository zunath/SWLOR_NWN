using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Perks: the definition caches, a creature's perk levels and the unlock. The stat bonuses are on
    /// <see cref="PerkStatBonusService"/>, the Force affinity on <see cref="PerkForceAffinityService"/>, the granted feats on
    /// <see cref="PerkGrantedFeatService"/> and the refunds on <see cref="PerkRefundService"/>.
    /// The logic that used to be static in the facade <c>Perk</c> is moved here unchanged; the event handler
    /// runs on the container's instance, and static callers reach it through <c>GameServices.Perk</c>. Calls into <c>Droid</c>
    /// and the definition search sit behind one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class PerkService : IPerkService
    {
        // All categories, including inactive
        private readonly Dictionary<PerkCategoryType, PerkCategoryAttribute> _allCategories = new();

        // Active categories only
        private readonly Dictionary<PerkGroupType, Dictionary<PerkCategoryType, PerkCategoryAttribute>> _activeCategories = new();

        // All perks, including inactive
        private readonly Dictionary<PerkType, PerkDetail> _allPerks = new();
        private readonly Dictionary<PerkCategoryType, List<PerkType>> _allPerksByCategory = new();

        // Active perks only
        private readonly Dictionary<PerkGroupType, Dictionary<PerkType, PerkDetail>> _activePerks = new();
        private readonly Dictionary<PerkCategoryType, Dictionary<PerkGroupType, Dictionary<PerkType, PerkDetail>>> _activePerksByCategory = new();

        // Trigger Actions
        private readonly Dictionary<PerkType, List<PerkTriggerEquippedAction>> _equipTriggers = new();
        private readonly Dictionary<PerkType, List<PerkTriggerUnequippedAction>> _unequipTriggers = new();
        private readonly Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> _purchaseTriggers = new();
        private readonly Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> _refundTriggers = new();

        // Perks with unlock requirements
        private readonly Dictionary<PerkType, PerkDetail> _perksWithUnlockRequirements = new();
        private readonly Dictionary<PerkType, int> _perkMaxLevels = new();
        private readonly Dictionary<CharacterType, CharacterTypeAttribute> _characterTypes = new();

        private readonly Dictionary<PerkType, Dictionary<int, int>> _perkLevelTiers = new();
        private readonly Dictionary<SkillType, List<PerkType>> _perksWithSkillRequirement = new();

        private readonly IDBService _db;
        private readonly ICompanionLookup _companions;

        public PerkService(
            IDBService db,
            ICompanionLookup companions)
        {
            _db = db;
            _companions = companions;
        }

        /// <summary>
        /// When the module loads, cache all perk and character type information.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            CachePerks();
            CacheCharacterTypes();
        }

        /// <summary>
        /// Caches perk information into various dictionaries for quicker look-ups later.
        /// </summary>
        private void CachePerks()
        {
            var categories = Enum.GetValues(typeof(PerkCategoryType)).Cast<PerkCategoryType>();
            foreach (var category in categories)
            {
                var categoryDetail = category.GetAttribute<PerkCategoryType, PerkCategoryAttribute>();
                _allCategories[category] = categoryDetail;
                _allPerksByCategory[category] = new List<PerkType>();

                if (categoryDetail.IsActive)
                {
                    _activePerksByCategory[category] = new Dictionary<PerkGroupType, Dictionary<PerkType, PerkDetail>>();
                }
            }

            // Organize perks to make later reads quicker.
            var types = GetPerkListDefinitionTypes();

            foreach (var type in types)
            {
                var instance = (IPerkListDefinition)Activator.CreateInstance(type);
                var perks = instance.BuildPerks();

                foreach (var (perkType, perkDetail) in perks)
                {
                    var categoryDetail = _allCategories[perkDetail.Category];

                    // Add to the perks cache
                    _allPerks[perkType] = perkDetail;

                    // Add to active cache if the perk is active
                    if (perkDetail.IsActive)
                    {
                        if (!_activePerks.ContainsKey(perkDetail.GroupType))
                            _activePerks[perkDetail.GroupType] = new Dictionary<PerkType, PerkDetail>();

                        _activePerks[perkDetail.GroupType][perkType] = perkDetail;

                        if (!_activePerksByCategory.ContainsKey(perkDetail.Category))
                            _activePerksByCategory[perkDetail.Category] = new Dictionary<PerkGroupType, Dictionary<PerkType, PerkDetail>>();

                        if (!_activePerksByCategory[perkDetail.Category].ContainsKey(perkDetail.GroupType))
                            _activePerksByCategory[perkDetail.Category][perkDetail.GroupType] = new Dictionary<PerkType, PerkDetail>();

                        _activePerksByCategory[perkDetail.Category][perkDetail.GroupType][perkType] = perkDetail;

                        // Add appropriate trigger entries if this perk is active and has them.
                        CacheTriggers(perkDetail);
                    }

                    // Add to active category cache if the perk and category are both active.
                    if (perkDetail.IsActive && categoryDetail.IsActive)
                    {
                        if (!_activeCategories.ContainsKey(perkDetail.GroupType))
                            _activeCategories[perkDetail.GroupType] = new Dictionary<PerkCategoryType, PerkCategoryAttribute>();

                        _activeCategories[perkDetail.GroupType][perkDetail.Category] = categoryDetail;
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
            }

            Console.WriteLine($"Loaded {_allPerks.Count} player perks.");
        }

        /// <summary>
        /// Caches character type information.
        /// </summary>
        private void CacheCharacterTypes()
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
            if (perk.RefundedTriggers.Count > 0)
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
            return _allPerks.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Every perk in the order the cache built them, without copying the table. The stat bonus tables are built from this.
        /// </summary>
        public IEnumerable<KeyValuePair<PerkType, PerkDetail>> EnumerateAllPerks()
        {
            return _allPerks;
        }

        /// <summary>
        /// The highest level of a perk, or 0 for a perk the catalog does not hold.
        /// </summary>
        public int GetPerkMaxLevel(PerkType perkType)
        {
            return _perkMaxLevels.TryGetValue(perkType, out var maxLevel) ? maxLevel : 0;
        }

        /// <summary>
        /// The perks that have a level requiring the skill, in the order the cache found them; empty when no perk needs it.
        /// </summary>
        public IReadOnlyList<PerkType> GetPerksWithSkillRequirement(SkillType skillType)
        {
            return _perksWithSkillRequirement.TryGetValue(skillType, out var perks)
                ? perks
                : Array.Empty<PerkType>();
        }

        /// <summary>
        /// Retrieves a list of all active perks, excluding inactive ones, by group.
        /// </summary>
        /// <returns>A list of all active perks.</returns>
        public Dictionary<PerkType, PerkDetail> GetAllActivePerks(PerkGroupType group)
        {
            return _activePerks[group]
                .ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all perk categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all perk categories.</returns>
        public Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllPerkCategories()
        {
            return _allCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all active perk categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of all active perk categories.</returns>
        public Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories(PerkGroupType group)
        {
            return _activeCategories[group]
                .ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all active perks by the specified category, by group.
        /// </summary>
        /// <param name="group">The group to filter by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of all active perks in the specified category.</returns>
        public Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkGroupType group, PerkCategoryType category)
        {
            return _activePerksByCategory[category][group]
                .ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves details about an individual perk.
        /// </summary>
        /// <param name="perkType">The type of perk to retrieve.</param>
        /// <returns>An object containing a perk's details.</returns>
        public PerkDetail GetPerkDetails(PerkType perkType)
        {
            return _allPerks[perkType];
        }

        public bool TryGetPerkDetails(PerkType perkType, out PerkDetail detail)
        {
            return _allPerks.TryGetValue(perkType, out detail);
        }

        public PerkCategoryType GetPerkCategoryType(PerkType perkType)
        {
            return _allPerks.TryGetValue(perkType, out var perkDetail)
                ? perkDetail.Category
                : PerkCategoryType.Invalid;
        }

        public bool IsPerkInCategory(PerkType perkType, int categoryValue)
        {
            if (perkType == PerkType.Invalid ||
                categoryValue <= 0 ||
                !Enum.IsDefined(typeof(PerkCategoryType), categoryValue))
            {
                return false;
            }

            return GetPerkCategoryType(perkType) == (PerkCategoryType)categoryValue;
        }

        /// <summary>
        /// Retrieves details about an individual perk category.
        /// </summary>
        /// <param name="categoryType">The type of category to retrieve.</param>
        /// <returns>An object containing a perk category's details.</returns>
        public PerkCategoryAttribute GetPerkCategoryDetails(PerkCategoryType categoryType)
        {
            return _allCategories[categoryType];
        }

        /// <summary>
        /// Retrieves the detail about a specific character type.
        /// </summary>
        /// <param name="characterType">The character type to retrieve.</param>
        /// <returns>A character type detail.</returns>
        public CharacterTypeAttribute GetCharacterType(CharacterType characterType)
        {
            return _characterTypes[characterType];
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
            if (perkType == PerkType.Invalid)
                return 0;

            if (GetIsDM(creature) && !GetIsDMPossessed(creature))
                return 0;

            // Players
            if (GetIsPC(creature) && !GetIsDMPossessed(creature))
            {
                return GetPlayerPerkLevel(creature, perkType);
            }
            // Droids
            else if (IsDroid(creature))
            {
                return GetDroidPerkLevel(creature, perkType);
            }
            // Beasts
            else if (_companions.IsPlayerBeast(creature))
            {
                return GetBeastPerkLevel(creature, perkType);
            }
            // Creatures or DM-possessed creatures
            else
            {
                var perkLevel = GetLocalInt(creature, $"PERK_LEVEL_{(int)perkType}");
                var perkMaxLevel = _perkMaxLevels[perkType];
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
            var beastId = _companions.GetBeastId(beast);
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

            dbPlayer.UnlockedPerks[perkType] = Clock.UtcNow;
            _db.Set(dbPlayer);
        }


        /// <summary>The perk list definitions of every loaded assembly, found lazily as the cache builds.</summary>
        protected virtual IEnumerable<Type> GetPerkListDefinitionTypes()
        {
            return GameAssemblies.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IPerkListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);
        }

        protected virtual bool IsDroid(uint creature)
        {
            return _companions.IsDroid(creature);
        }

        protected virtual int GetDroidPerkLevel(uint droid, PerkType perkType)
        {
            return _companions.GetDroidPerkLevel(droid, perkType);
        }
    }
}
