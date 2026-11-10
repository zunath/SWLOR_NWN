using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Perks: the definition caches, a creature's perk levels, the stat bonuses and granted feats that follow from them,
    /// and refunds. The logic that used to be static in the facade <c>Perk</c> is moved here unchanged; the event handlers
    /// run on the container's instance, and static callers reach it through <c>GameServices.Perk</c>. Calls into <c>Stat</c>,
    /// <c>Ability</c>, <c>Droid</c> and <c>Combat</c>, and the definition search, sit behind one-line
    /// <c>protected virtual</c> methods whose bodies are the original calls.
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
        private readonly Dictionary<StatType, List<StatBonusGroup>> _statBonusGroupsByStat = new();
        private readonly Dictionary<StatType, List<TargetedStatBonusGroup>> _targetedStatBonusGroupsByAdjustmentStat = new();
        private readonly Dictionary<PerkType, Dictionary<int, FeatType[]>> _grantedFeatsByPerkLevel = new();
        private readonly Dictionary<PerkType, Dictionary<int, HashSet<FeatType>>> _grantedFeatSetsByPerkLevel = new();
        private readonly Dictionary<PerkType, Dictionary<int, FeatType[]>> _currentActiveAbilityFeatsByPerkLevel = new();
        private readonly Dictionary<PerkType, Dictionary<int, HashSet<FeatType>>> _currentActiveAbilityFeatSetsByPerkLevel = new();
        private readonly Dictionary<PerkType, FeatType[]> _allActiveAbilityFeatsByPerk = new();
        private readonly Dictionary<PerkType, RecastGroup> _activeAbilityRecastGroupByPerk = new();
        private readonly HashSet<(PerkType PerkType, FeatType Feat)> _activeAbilityFeatsByPerk = new();
        private static readonly FrozenSet<FeatType> _emptyFeatSet = FrozenSet<FeatType>.Empty;
        private bool _perkFeatCacheLoaded;
        private const int ForceAffinityMinimum = -10;
        private const int ForceAffinityMaximum = 10;
        // Heals, shields and regeneration gain at most +25% from affinity; damage and accuracy keep the full range.
        private const float ForceAffinityRestorativeMaximumMultiplier = 1.25f;

        private class StatBonusGroup
        {
            public StatBonusGroup(PerkType perkType, PerkDetail perkDetail)
            {
                PerkType = perkType;
                PerkDetail = perkDetail;
            }

            public PerkType PerkType { get; }
            public PerkDetail PerkDetail { get; }
            public List<PerkStatBonus> PerkBonuses { get; } = new();
            public Dictionary<int, List<PerkStatBonus>> LevelBonuses { get; } = new();
        }

        private class TargetedStatBonusGroup
        {
            public TargetedStatBonusGroup(PerkType perkType)
            {
                PerkType = perkType;
            }

            public PerkType PerkType { get; }
            public Dictionary<int, Dictionary<StatType, List<PerkStatBonus>>> BonusesByLevel { get; } = new();
        }

        private readonly IDBService _db;
        private readonly IQuickCastService _quickCast;
        private readonly IBeastMasteryService _beastMastery;
        private readonly IStatusEffectService _statusEffect;
        private readonly Lazy<IAbilityService> _ability;
        private readonly Lazy<IStatService> _stat;
        private readonly Lazy<ICombatService> _combat;

        private readonly Lazy<IDroidService> _droid;

        public PerkService(
            IDBService db,
            IQuickCastService quickCast,
            IBeastMasteryService beastMastery,
            IStatusEffectService statusEffect,
            Lazy<IAbilityService> ability,
            Lazy<IStatService> stat,
            Lazy<ICombatService> combat,
            Lazy<IDroidService> droid)
        {
            _droid = droid;
            _db = db;
            _quickCast = quickCast;
            _beastMastery = beastMastery;
            _statusEffect = statusEffect;
            _ability = ability;
            _stat = stat;
            _combat = combat;
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

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void CachePerkFeatData()
        {
            CachePerkFeatLookups();
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
                    CacheStatBonusPerk(perkType, perkDetail);
                }
            }

            _perkFeatCacheLoaded = false;
            Console.WriteLine($"Loaded {_allPerks.Count} player perks.");
        }

        private void CacheStatBonusPerk(PerkType perkType, PerkDetail perkDetail)
        {
            foreach (var statBonus in perkDetail.StatBonuses)
            {
                GetOrCreateStatBonusGroup(statBonus.Stat, perkType, perkDetail).PerkBonuses.Add(statBonus);
            }

            foreach (var (level, perkLevel) in perkDetail.PerkLevels)
            {
                if (perkLevel.StatBonuses.Count <= 0)
                    continue;

                var levelBonusesByStat = new Dictionary<StatType, List<PerkStatBonus>>();
                foreach (var statBonus in perkLevel.StatBonuses)
                {
                    var statBonusGroup = GetOrCreateStatBonusGroup(statBonus.Stat, perkType, perkDetail);
                    if (!statBonusGroup.LevelBonuses.TryGetValue(level, out var levelBonuses))
                    {
                        levelBonuses = new List<PerkStatBonus>();
                        statBonusGroup.LevelBonuses[level] = levelBonuses;
                    }

                    levelBonuses.Add(statBonus);

                    if (!levelBonusesByStat.TryGetValue(statBonus.Stat, out var bonusesByStat))
                    {
                        bonusesByStat = new List<PerkStatBonus>();
                        levelBonusesByStat[statBonus.Stat] = bonusesByStat;
                    }

                    bonusesByStat.Add(statBonus);
                }

                foreach (var adjustmentStat in levelBonusesByStat.Keys)
                {
                    var targetedStatBonusGroup = GetOrCreateTargetedStatBonusGroup(adjustmentStat, perkType);
                    targetedStatBonusGroup.BonusesByLevel[level] = levelBonusesByStat;
                }
            }
        }

        private StatBonusGroup GetOrCreateStatBonusGroup(
            StatType stat,
            PerkType perkType,
            PerkDetail perkDetail)
        {
            if (!_statBonusGroupsByStat.TryGetValue(stat, out var statBonusGroups))
            {
                statBonusGroups = new List<StatBonusGroup>();
                _statBonusGroupsByStat[stat] = statBonusGroups;
            }

            var statBonusGroup = statBonusGroups.SingleOrDefault(x => x.PerkType == perkType);
            if (statBonusGroup != null)
                return statBonusGroup;

            statBonusGroup = new StatBonusGroup(perkType, perkDetail);
            statBonusGroups.Add(statBonusGroup);
            return statBonusGroup;
        }

        private TargetedStatBonusGroup GetOrCreateTargetedStatBonusGroup(
            StatType adjustmentStat,
            PerkType perkType)
        {
            if (!_targetedStatBonusGroupsByAdjustmentStat.TryGetValue(adjustmentStat, out var targetedStatBonusGroups))
            {
                targetedStatBonusGroups = new List<TargetedStatBonusGroup>();
                _targetedStatBonusGroupsByAdjustmentStat[adjustmentStat] = targetedStatBonusGroups;
            }

            var targetedStatBonusGroup = targetedStatBonusGroups.SingleOrDefault(x => x.PerkType == perkType);
            if (targetedStatBonusGroup != null)
                return targetedStatBonusGroup;

            targetedStatBonusGroup = new TargetedStatBonusGroup(perkType);
            targetedStatBonusGroups.Add(targetedStatBonusGroup);
            return targetedStatBonusGroup;
        }

        private void CachePerkFeatLookups()
        {
            _grantedFeatsByPerkLevel.Clear();
            _grantedFeatSetsByPerkLevel.Clear();
            _currentActiveAbilityFeatsByPerkLevel.Clear();
            _currentActiveAbilityFeatSetsByPerkLevel.Clear();
            _allActiveAbilityFeatsByPerk.Clear();
            _activeAbilityRecastGroupByPerk.Clear();
            _activeAbilityFeatsByPerk.Clear();

            var abilityDetails = GetAllAbilityDetails();
            if (abilityDetails.Count <= 0)
            {
                _perkFeatCacheLoaded = false;
                return;
            }

            foreach (var (perkType, perkDetail) in _allPerks)
            {
                if (!_perkMaxLevels.TryGetValue(perkType, out var maxLevel) || maxLevel <= 0)
                    continue;

                var activeAbilityFeatsByLevel = BuildActiveAbilityFeatsByLevel(perkType, perkDetail, abilityDetails);
                var activeAbilityRecastGroup = FindActiveAbilityRecastGroup(activeAbilityFeatsByLevel, abilityDetails);
                var allActiveAbilityFeats = new List<FeatType>();

                if (activeAbilityRecastGroup != RecastGroup.Invalid)
                {
                    _activeAbilityRecastGroupByPerk[perkType] = activeAbilityRecastGroup;
                }

                foreach (var (_, activeAbilityFeats) in activeAbilityFeatsByLevel.OrderBy(x => x.Key))
                {
                    foreach (var feat in activeAbilityFeats)
                    {
                        AddDistinct(allActiveAbilityFeats, feat);
                        _activeAbilityFeatsByPerk.Add((perkType, feat));
                    }
                }

                if (allActiveAbilityFeats.Count > 0)
                {
                    _allActiveAbilityFeatsByPerk[perkType] = allActiveAbilityFeats.ToArray();
                }

                CachePerkLevelFeatLookups(perkType, perkDetail, maxLevel, activeAbilityFeatsByLevel);
            }

            _perkFeatCacheLoaded = true;
            Console.WriteLine($"Loaded active ability feat lookups for {_allActiveAbilityFeatsByPerk.Count} perks.");
        }

        private Dictionary<int, List<FeatType>> BuildActiveAbilityFeatsByLevel(
            PerkType perkType,
            PerkDetail perkDetail,
            IReadOnlyDictionary<FeatType, AbilityDetail> abilityDetails)
        {
            var result = new Dictionary<int, List<FeatType>>();

            foreach (var (level, levelDetail) in perkDetail.PerkLevels)
            {
                foreach (var feat in levelDetail.GrantedFeats)
                {
                    if (!IsActiveAbilityFeatForPerk(perkType, feat, abilityDetails))
                        continue;

                    if (!result.ContainsKey(level))
                    {
                        result[level] = new List<FeatType>();
                    }

                    AddDistinct(result[level], feat);
                }
            }

            return result;
        }

        private RecastGroup FindActiveAbilityRecastGroup(
            IReadOnlyDictionary<int, List<FeatType>> activeAbilityFeatsByLevel,
            IReadOnlyDictionary<FeatType, AbilityDetail> abilityDetails)
        {
            foreach (var (_, activeAbilityFeats) in activeAbilityFeatsByLevel.OrderBy(x => x.Key))
            {
                foreach (var feat in activeAbilityFeats)
                {
                    var recastGroup = abilityDetails[feat].RecastGroup;
                    if (recastGroup != RecastGroup.Invalid)
                    {
                        return recastGroup;
                    }
                }
            }

            return RecastGroup.Invalid;
        }

        private bool IsActiveAbilityFeatForPerk(
            PerkType perkType,
            FeatType feat,
            IReadOnlyDictionary<FeatType, AbilityDetail> abilityDetails)
        {
            return perkType != PerkType.Invalid &&
                   abilityDetails.TryGetValue(feat, out var ability) &&
                   ability.EffectiveLevelPerkType == perkType;
        }

        private void CachePerkLevelFeatLookups(
            PerkType perkType,
            PerkDetail perkDetail,
            int maxLevel,
            IReadOnlyDictionary<int, List<FeatType>> activeAbilityFeatsByLevel)
        {
            var grantedFeatsByLevel = new Dictionary<int, FeatType[]>();
            var grantedFeatSetsByLevel = new Dictionary<int, HashSet<FeatType>>();
            var currentActiveAbilityFeatsByLevel = new Dictionary<int, FeatType[]>();
            var currentActiveAbilityFeatSetsByLevel = new Dictionary<int, HashSet<FeatType>>();
            var perkLevels = perkDetail.PerkLevels.OrderBy(x => x.Key).ToList();

            for (var level = 1; level <= maxLevel; level++)
            {
                var grantedFeats = new List<FeatType>();

                foreach (var (_, levelDetail) in perkLevels.Where(x => x.Key <= level))
                {
                    foreach (var feat in levelDetail.GrantedFeats)
                    {
                        if (_activeAbilityFeatsByPerk.Contains((perkType, feat)))
                            continue;

                        AddDistinct(grantedFeats, feat);
                    }
                }

                var currentActiveAbilityFeats = FindCurrentActiveAbilityFeats(activeAbilityFeatsByLevel, level);
                foreach (var feat in currentActiveAbilityFeats)
                {
                    AddDistinct(grantedFeats, feat);
                }

                grantedFeatsByLevel[level] = grantedFeats.ToArray();
                grantedFeatSetsByLevel[level] = grantedFeats.ToHashSet();
                currentActiveAbilityFeatsByLevel[level] = currentActiveAbilityFeats.ToArray();
                currentActiveAbilityFeatSetsByLevel[level] = currentActiveAbilityFeats.ToHashSet();
            }

            _grantedFeatsByPerkLevel[perkType] = grantedFeatsByLevel;
            _grantedFeatSetsByPerkLevel[perkType] = grantedFeatSetsByLevel;
            _currentActiveAbilityFeatsByPerkLevel[perkType] = currentActiveAbilityFeatsByLevel;
            _currentActiveAbilityFeatSetsByPerkLevel[perkType] = currentActiveAbilityFeatSetsByLevel;
        }

        private IReadOnlyList<FeatType> FindCurrentActiveAbilityFeats(
            IReadOnlyDictionary<int, List<FeatType>> activeAbilityFeatsByLevel,
            int perkLevel)
        {
            foreach (var (level, feats) in activeAbilityFeatsByLevel.OrderByDescending(x => x.Key))
            {
                if (level <= perkLevel)
                    return feats;
            }

            return Array.Empty<FeatType>();
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

        public int GetForceAffinity(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature))
                return 0;

            return Math.Clamp(
                GetCreatureStatAdjustment(creature, StatType.ForceAffinity),
                ForceAffinityMinimum,
                ForceAffinityMaximum);
        }

        public bool TryGetForceSideAffinity(uint creature, PerkType perkType, out int sideAffinity)
        {
            sideAffinity = 0;

            if (perkType == PerkType.Invalid ||
                !_allPerks.TryGetValue(perkType, out var detail))
            {
                return false;
            }

            if (detail.ForceAffinityType == null)
                return false;

            var forceAffinity = GetForceAffinity(creature);
            switch (detail.ForceAffinityType.Value)
            {
                case ForceAffinityType.Light:
                    sideAffinity = forceAffinity;
                    return true;
                case ForceAffinityType.Dark:
                    sideAffinity = -forceAffinity;
                    return true;
                default:
                    return false;
            }
        }

        public float GetForceAffinityMagnitudeMultiplier(uint creature, PerkType perkType)
        {
            return TryGetForceSideAffinity(creature, perkType, out var sideAffinity)
                ? Math.Clamp(1f + 0.05f * sideAffinity, 0.5f, 1.5f)
                : 1f;
        }

        public int GetForceAffinityHitChanceAdjustment(uint creature, PerkType perkType)
        {
            return TryGetForceSideAffinity(creature, perkType, out var sideAffinity)
                ? (int)Math.Floor(sideAffinity / 2f)
                : 0;
        }

        public float GetForceAffinityRestorativeMagnitudeMultiplier(uint creature, PerkType perkType)
        {
            return Math.Min(GetForceAffinityMagnitudeMultiplier(creature, perkType), ForceAffinityRestorativeMaximumMultiplier);
        }

        public int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount)
        {
            return amount <= 0 ? amount : ApplyMagnitudeMultiplier(amount, GetForceAffinityMagnitudeMultiplier(creature, perkType));
        }

        /// <summary>Applies affinity to a heal, shield or regeneration amount, which gains at most +25%.</summary>
        public int ApplyForceAffinityRestorativeMagnitude(uint creature, PerkType perkType, int amount)
        {
            return amount <= 0 ? amount : ApplyMagnitudeMultiplier(amount, GetForceAffinityRestorativeMagnitudeMultiplier(creature, perkType));
        }

        private int ApplyMagnitudeMultiplier(int amount, float multiplier)
        {
            if (Math.Abs(multiplier - 1f) < 0.001f)
                return amount;

            return Math.Max(1, (int)Math.Round(amount * multiplier, MidpointRounding.AwayFromZero));
        }

        public float ApplyForceAffinityMagnitude(uint creature, PerkType perkType, float amount)
        {
            if (amount <= 0f)
                return amount;

            return amount * GetForceAffinityMagnitudeMultiplier(creature, perkType);
        }

        public IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            if (!_statBonusGroupsByStat.TryGetValue(payloadStat, out var groups))
                return Array.Empty<StatAdjustmentSource>();

            List<StatAdjustmentSource> sources = null;
            foreach (var group in groups)
            {
                var level = GetStatBonusPerkLevel(creature, group.PerkType);
                if (level <= 0 || !group.PerkDetail.PerkLevels.TryGetValue(level, out var perkLevel))
                    continue;

                var stats = new Dictionary<StatType, int>();
                foreach (var bonus in group.PerkDetail.StatBonuses)
                {
                    stats.TryGetValue(bonus.Stat, out var current);
                    stats[bonus.Stat] = AggregateStatAdjustment(bonus.Stat, current, bonus.Calculate(creature));
                }
                foreach (var bonus in perkLevel.StatBonuses)
                {
                    stats.TryGetValue(bonus.Stat, out var current);
                    stats[bonus.Stat] = AggregateStatAdjustment(bonus.Stat, current, bonus.Calculate(creature));
                }

                if (stats.TryGetValue(payloadStat, out var value) && value != 0)
                {
                    sources ??= new List<StatAdjustmentSource>();
                    sources.Add(new StatAdjustmentSource($"perk:{(int)group.PerkType}", stats));
                }
            }
            return (IReadOnlyList<StatAdjustmentSource>)sources ?? Array.Empty<StatAdjustmentSource>();
        }

        public int GetStatBonus(uint creature, StatType stat)
        {
            var bonus = 0;
            if (!_statBonusGroupsByStat.TryGetValue(stat, out var statBonusGroups))
                return bonus;

            foreach (var statBonusGroup in statBonusGroups)
            {
                var level = GetStatBonusPerkLevel(creature, statBonusGroup.PerkType);
                if (level <= 0 || !statBonusGroup.PerkDetail.PerkLevels.ContainsKey(level))
                    continue;

                foreach (var statBonus in statBonusGroup.PerkBonuses)
                {
                    bonus = AggregateStatAdjustment(stat, bonus, statBonus.Calculate(creature));
                }

                if (!statBonusGroup.LevelBonuses.TryGetValue(level, out var levelBonuses))
                    continue;

                foreach (var statBonus in levelBonuses)
                {
                    bonus = AggregateStatAdjustment(stat, bonus, statBonus.Calculate(creature));
                }
            }

            return bonus;
        }

        public int GetTargetedStatBonus(
            uint creature,
            PerkType targetPerkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (targetPerkType == PerkType.Invalid)
                return 0;

            var bonus = 0;
            if (!_targetedStatBonusGroupsByAdjustmentStat.TryGetValue(adjustmentStatType, out var targetedStatBonusGroups))
                return bonus;

            foreach (var targetedStatBonusGroup in targetedStatBonusGroups)
            {
                var level = GetStatBonusPerkLevel(creature, targetedStatBonusGroup.PerkType);
                if (level <= 0 || !targetedStatBonusGroup.BonusesByLevel.TryGetValue(level, out var bonusesByStat))
                    continue;

                var primaryPerkValue = CalculateStatBonuses(creature, bonusesByStat, primaryPerkStatType);
                var secondaryPerkValue = CalculateStatBonuses(creature, bonusesByStat, secondaryPerkStatType);
                var adjustment = CalculateStatBonuses(creature, bonusesByStat, adjustmentStatType);

                if (adjustment != 0 && IsTargetedPerk(targetPerkType, primaryPerkValue, secondaryPerkValue))
                    bonus += adjustment;
            }

            return bonus;
        }

        private int CalculateStatBonuses(
            uint creature,
            IReadOnlyDictionary<StatType, List<PerkStatBonus>> bonusesByStat,
            StatType stat)
        {
            if (!bonusesByStat.TryGetValue(stat, out var statBonuses))
                return 0;

            var bonus = 0;
            foreach (var statBonus in statBonuses)
            {
                bonus += statBonus.Calculate(creature);
            }

            return bonus;
        }

        private int GetStatBonusPerkLevel(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            if (GetIsPC(creature) && !GetIsDMPossessed(creature) ||
                IsDroid(creature) ||
                _beastMastery.IsPlayerBeast(creature))
            {
                return GetPerkLevel(creature, perkType);
            }

            return GetLocalInt(creature, $"PERK_LEVEL_{(int)perkType}");
        }

        private bool IsTargetedPerk(PerkType targetPerkType, int primaryPerkValue, int secondaryPerkValue)
        {
            return targetPerkType == GetPerkTypeFromStat(primaryPerkValue) ||
                   targetPerkType == GetPerkTypeFromStat(secondaryPerkValue);
        }

        private PerkType GetPerkTypeFromStat(int value)
        {
            return value > 0 && System.Enum.IsDefined(typeof(PerkType), value)
                ? (PerkType)value
                : PerkType.Invalid;
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
                var controller = GetDroidControllerItem(creature);
                var droidDetails = LoadDroidItemPropertyDetails(controller);

                return droidDetails.Perks.TryGetValue(perkType, out var droidPerkLevel)
                    ? droidPerkLevel
                    : 0;
            }
            // Beasts
            else if (_beastMastery.IsPlayerBeast(creature))
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

        public IReadOnlyList<FeatType> GetGrantedFeatsForPerkLevel(PerkType perkType, int perkLevel)
        {
            EnsurePerkFeatCacheLoaded();
            return GetCachedFeatList(_grantedFeatsByPerkLevel, perkType, perkLevel);
        }

        public IReadOnlyList<FeatType> GetCurrentActiveAbilityFeats(PerkType perkType, int perkLevel)
        {
            EnsurePerkFeatCacheLoaded();
            return GetCachedFeatList(_currentActiveAbilityFeatsByPerkLevel, perkType, perkLevel);
        }

        public IReadOnlyList<FeatType> GetAllActiveAbilityFeats(PerkType perkType)
        {
            EnsurePerkFeatCacheLoaded();
            return _allActiveAbilityFeatsByPerk.TryGetValue(perkType, out var feats)
                ? feats
                : Array.Empty<FeatType>();
        }

        public RecastGroup GetActiveAbilityRecastGroup(PerkType perkType)
        {
            EnsurePerkFeatCacheLoaded();
            return _activeAbilityRecastGroupByPerk.TryGetValue(perkType, out var recastGroup)
                ? recastGroup
                : RecastGroup.Invalid;
        }

        /// <summary>
        /// Restores native feats from purchased perks after loading or migrating a character file.
        /// This does not repeat purchase triggers or change the player's quickbar layout.
        /// </summary>
        public void RestorePlayerFeats(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            foreach (var (perkType, level) in dbPlayer.Perks)
            {
                if (level <= 0 ||
                    !_allPerks.TryGetValue(perkType, out var detail) ||
                    !detail.IsActive || detail.GroupType != PerkGroupType.Player)
                    continue;

                SyncGrantedFeats(player, perkType, level, true);
            }
        }

        public void SyncGrantedFeats(uint creature, PerkType perkType, int perkLevel, bool addByLevel)
        {
            if (!GetIsObjectValid(creature))
                return;

            var grantedFeats = GetGrantedFeatsForPerkLevel(perkType, perkLevel);
            var grantedFeatSet = GetGrantedFeatSetForPerkLevel(perkType, perkLevel);

            foreach (var feat in GetAllActiveAbilityFeats(perkType))
            {
                if (grantedFeatSet.Contains(feat))
                    continue;

                _quickCast.RemoveFeat(creature, feat);
            }

            foreach (var feat in grantedFeats)
            {
                if (_quickCast.KnowsFeat(creature, feat))
                    continue;

                if (addByLevel)
                {
                    CreaturePlugin.AddFeatByLevel(creature, feat, 1);
                }
                else
                {
                    CreaturePlugin.AddFeat(creature, feat);
                }
            }

            _quickCast.ApplyPreferences(creature);
        }

        /// <summary>
        /// Removes declared self effects and effects granted by the refunded perk's abilities.
        /// Source-owned effects retain their ability identity even when another perk uses the same status class.
        /// </summary>
        public void RemoveStatusEffectsOnPerkRefund(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid || !GetIsObjectValid(creature))
                return;

            var statusEffectTypes = GetAllAbilityDetails()
                .Values
                .Where(ability => ability.EffectiveLevelPerkType == perkType)
                .SelectMany(ability => ability.StatusEffectTypesRemovedOnPerkRefund)
                .Distinct()
                .ToList();

            foreach (var statusEffectType in statusEffectTypes)
            {
                _statusEffect.RemoveStatusEffect(creature, statusEffectType, false);
            }

            var sourceOwnedStatusEffects = GetSourceOwnedStatusEffectRefunds(
                GetAllAbilityDetails().Values, perkType);

            foreach (var sourceOwnedStatusEffect in sourceOwnedStatusEffects)
            {
                _statusEffect.RemoveStatusEffectsFromAllTargetsBySource(
                    creature,
                    sourceOwnedStatusEffect.StatusEffectType,
                    false,
                    sourceOwnedStatusEffect.Ability);
            }

            RefreshStatDrivenTrackerEffects(creature);
        }

        /// <summary>
        /// Selects distinct ability/status pairs for a perk refund. Deduplication retains separate
        /// abilities that share a status class so each cleanup uses its own originating ability.
        /// </summary>
        public IReadOnlyList<(AbilityDetail Ability, Type StatusEffectType)> GetSourceOwnedStatusEffectRefunds(
            IEnumerable<AbilityDetail> abilities, PerkType perkType)
        {
            return abilities
                .Where(ability => ability.EffectiveLevelPerkType == perkType)
                .SelectMany(ability => ability.SourceOwnedStatusEffectTypesRemovedOnPerkRefund
                    .Select(statusEffectType => (Ability: ability, StatusEffectType: statusEffectType)))
                .Distinct()
                .ToArray();
        }

        public bool ShouldEnforceActiveAbilityFeatReplacement(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid || !GetIsObjectValid(creature))
                return false;

            if (GetIsPC(creature) && !GetIsDMPossessed(creature))
                return true;

            if (IsDroid(creature) || _beastMastery.IsPlayerBeast(creature))
                return true;

            return GetLocalInt(creature, $"PERK_LEVEL_{(int)perkType}") > 0;
        }

        public bool IsCurrentActiveAbilityFeat(FeatType feat, PerkType perkType, int perkLevel)
        {
            if (!IsReplacingActiveAbilityFeat(perkType, feat))
                return true;

            var currentActiveAbilityFeats = GetCurrentActiveAbilityFeatSet(perkType, perkLevel);
            return currentActiveAbilityFeats.Count <= 0 ||
                   currentActiveAbilityFeats.Contains(feat);
        }

        private IReadOnlySet<FeatType> GetGrantedFeatSetForPerkLevel(PerkType perkType, int perkLevel)
        {
            EnsurePerkFeatCacheLoaded();
            return GetCachedFeatSet(_grantedFeatSetsByPerkLevel, perkType, perkLevel);
        }

        private IReadOnlySet<FeatType> GetCurrentActiveAbilityFeatSet(PerkType perkType, int perkLevel)
        {
            EnsurePerkFeatCacheLoaded();
            return GetCachedFeatSet(_currentActiveAbilityFeatSetsByPerkLevel, perkType, perkLevel);
        }

        private IReadOnlyList<FeatType> GetCachedFeatList(
            Dictionary<PerkType, Dictionary<int, FeatType[]>> cache,
            PerkType perkType,
            int perkLevel)
        {
            var cacheLevel = NormalizePerkFeatCacheLevel(perkType, perkLevel);
            if (cacheLevel <= 0)
                return Array.Empty<FeatType>();

            return cache.TryGetValue(perkType, out var levelCache) &&
                   levelCache.TryGetValue(cacheLevel, out var feats)
                ? feats
                : Array.Empty<FeatType>();
        }

        private IReadOnlySet<FeatType> GetCachedFeatSet(
            Dictionary<PerkType, Dictionary<int, HashSet<FeatType>>> cache,
            PerkType perkType,
            int perkLevel)
        {
            var cacheLevel = NormalizePerkFeatCacheLevel(perkType, perkLevel);
            if (cacheLevel <= 0)
                return _emptyFeatSet;

            return cache.TryGetValue(perkType, out var levelCache) &&
                   levelCache.TryGetValue(cacheLevel, out var feats)
                ? feats
                : _emptyFeatSet;
        }

        private int NormalizePerkFeatCacheLevel(PerkType perkType, int perkLevel)
        {
            if (perkLevel <= 0)
                return 0;

            if (!_perkMaxLevels.TryGetValue(perkType, out var maxLevel))
                return perkLevel;

            return perkLevel > maxLevel
                ? maxLevel
                : perkLevel;
        }

        private void EnsurePerkFeatCacheLoaded()
        {
            if (_perkFeatCacheLoaded || _allPerks.Count <= 0)
                return;

            if (GetAllAbilityDetails().Count <= 0)
                return;

            CachePerkFeatLookups();
        }

        private bool IsReplacingActiveAbilityFeat(PerkType perkType, FeatType feat)
        {
            EnsurePerkFeatCacheLoaded();
            return _activeAbilityFeatsByPerk.Contains((perkType, feat));
        }

        private void AddDistinct(List<FeatType> feats, FeatType feat)
        {
            if (!feats.Contains(feat))
            {
                feats.Add(feat);
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
            var beastId = _beastMastery.GetBeastId(beast);
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

        /// <summary>
        /// When a skill receives decay, any perks tied to that skill should be checked.
        /// If the player no longer meets the requirements for those perks, they should be reduced in level.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorLoseSkill)]
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
                        _quickCast.RemoveFeat(player, feat);
                    }

                    Log.Write(LogGroup.PerkRefund, $"AUTOMATIC DECAY REFUND - {playerId} - Refunded Date {Clock.UtcNow} - Level {perkLevel} - PerkID {perkType}");
                    FloatingTextStringOnCreature($"Perk '{perkDetail.Name}' level {level} was refunded because your skill fell under the minimum requirements. You reclaimed {perkLevel.Price} SP.", player, false);
                }

                dbPlayer.Perks[perkType] = effectiveLevel;
                if (effectiveLevel > 0)
                {
                    SyncGrantedFeats(player, perkType, effectiveLevel, true);
                }

                _db.Set(dbPlayer);

                RemoveStatusEffectsOnPerkRefund(player, perkType);

                foreach (var refundTrigger in perkDetail.RefundedTriggers)
                {
                    refundTrigger(player);
                }
            }

            ExportSingleCharacter(player);
        }

        /// <summary>The perk list definitions of every loaded assembly, found lazily as the cache builds.</summary>
        protected virtual IEnumerable<Type> GetPerkListDefinitionTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IPerkListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);
        }

        protected virtual IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails()
        {
            return _ability.Value.GetAllAbilityDetails();
        }

        protected virtual int GetCreatureStatAdjustment(uint creature, StatType stat)
        {
            return _stat.Value.GetStatAdjustment(creature, stat);
        }

        protected virtual int AggregateStatAdjustment(StatType stat, int current, int adjustment)
        {
            return _stat.Value.AggregateStatAdjustment(stat, current, adjustment);
        }

        protected virtual bool IsDroid(uint creature)
        {
            return _droid.Value.IsDroid(creature);
        }

        protected virtual uint GetDroidControllerItem(uint droid)
        {
            return _droid.Value.GetControllerItem(droid);
        }

        protected virtual DroidItemPropertyDetails LoadDroidItemPropertyDetails(uint controller)
        {
            return _droid.Value.LoadDroidItemPropertyDetails(controller);
        }

        protected virtual void RefreshStatDrivenTrackerEffects(uint creature)
        {
            _combat.Value.RefreshStatDrivenTrackerEffects(creature);
        }
    }
}
