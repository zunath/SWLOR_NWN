using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The feats perks grant: the lookup tables built from every perk and ability when the module loads, the active ability feat
    /// a perk level replaces, and the sync that adds a perk level's feats to a creature or takes a replaced one away. They moved
    /// here from <see cref="PerkService"/> unchanged except where they read the catalog tables, which they now ask the stay-part
    /// for (<see cref="IPerkService.EnumerateAllPerks"/>, <see cref="IPerkService.GetPerkMaxLevel"/>). Calls into the hot bar and
    /// the ability catalog sit behind one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class PerkGrantedFeatService : IPerkGrantedFeatService
    {
        private readonly Dictionary<PerkType, Dictionary<int, FeatType[]>> _grantedFeatsByPerkLevel = new();
        private readonly Dictionary<PerkType, Dictionary<int, HashSet<FeatType>>> _grantedFeatSetsByPerkLevel = new();
        private readonly Dictionary<PerkType, Dictionary<int, FeatType[]>> _currentActiveAbilityFeatsByPerkLevel = new();
        private readonly Dictionary<PerkType, Dictionary<int, HashSet<FeatType>>> _currentActiveAbilityFeatSetsByPerkLevel = new();
        private readonly Dictionary<PerkType, FeatType[]> _allActiveAbilityFeatsByPerk = new();
        private readonly Dictionary<PerkType, RecastGroup> _activeAbilityRecastGroupByPerk = new();
        private readonly HashSet<(PerkType PerkType, FeatType Feat)> _activeAbilityFeatsByPerk = new();
        private static readonly FrozenSet<FeatType> _emptyFeatSet = FrozenSet<FeatType>.Empty;
        private bool _perkFeatCacheLoaded;

        private readonly IDBService _db;
        private readonly IFeatHotBar _quickCast;
        private readonly ICompanionLookup _companions;
        private readonly IAbilityCatalog _catalog;
        private readonly IPerkService _perk;

        public PerkGrantedFeatService(
            IDBService db,
            IFeatHotBar quickCast,
            ICompanionLookup companions,
            IAbilityCatalog catalog,
            IPerkService perk)
        {
            _db = db;
            _quickCast = quickCast;
            _companions = companions;
            _catalog = catalog;
            _perk = perk;
        }

        /// <summary>
        /// When the module loads, forgets the feat lookups of an earlier cache build so the next read builds them again from
        /// the perks the catalog cached. It runs right after <see cref="PerkService.CacheData"/>, where the catalog build used to clear the flag.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void ResetPerkFeatCache()
        {
            _perkFeatCacheLoaded = false;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void CachePerkFeatData()
        {
            CachePerkFeatLookups();
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

            foreach (var (perkType, perkDetail) in _perk.EnumerateAllPerks())
            {
                var maxLevel = _perk.GetPerkMaxLevel(perkType);
                if (maxLevel <= 0)
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
                    !_perk.TryGetPerkDetails(perkType, out var detail) ||
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

        public bool ShouldEnforceActiveAbilityFeatReplacement(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid || !GetIsObjectValid(creature))
                return false;

            if (GetIsPC(creature) && !GetIsDMPossessed(creature))
                return true;

            if (IsDroid(creature) || _companions.IsPlayerBeast(creature))
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

            var maxLevel = _perk.GetPerkMaxLevel(perkType);
            if (maxLevel <= 0)
                return perkLevel;

            return perkLevel > maxLevel
                ? maxLevel
                : perkLevel;
        }

        private void EnsurePerkFeatCacheLoaded()
        {
            if (_perkFeatCacheLoaded || !_perk.EnumerateAllPerks().Any())
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

        protected virtual IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails()
        {
            return _catalog.GetAllAbilityDetails();
        }

        protected virtual bool IsDroid(uint creature)
        {
            return _companions.IsDroid(creature);
        }
    }
}
