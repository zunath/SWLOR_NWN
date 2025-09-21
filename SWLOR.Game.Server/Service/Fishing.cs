
using System.Collections.Generic;
using System;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Service
{
    public class Fishing
    {
        private readonly IDatabaseService _db;
        private readonly IGenericCacheService _cacheService;
        private readonly IItemCacheService _itemCache;
        private readonly IRandomService _random;
        private readonly ISkillService _skillService;
        private readonly IActivityService _activityService;
        private readonly IMessagingService _messagingService;
        
        // Cached data
        private IEnumCache<FishType, FishAttribute> _fishCache;
        private IEnumCache<FishingRodType, FishingRodAttribute> _rodCache;
        private IEnumCache<FishingBaitType, FishingBaitAttribute> _baitCache;

        private readonly Dictionary<string, FishingRodType> _rodsByResref = new();
        private readonly Dictionary<string, FishingBaitType> _baitsByResref = new();
        private readonly Dictionary<FishingLocationType, FishingLocationDetail> _fishingLocations = new();
        private readonly Dictionary<FishingLocationType, List<string>> _fishResrefsByLocation = new();

        public Fishing(
            IDatabaseService db,
            IGenericCacheService cacheService,
            IItemCacheService itemCache,
            IRandomService random,
            ISkillService skillService,
            IActivityService activityService,
            IMessagingService messagingService)
        {
            _db = db;
            _cacheService = cacheService;
            _itemCache = itemCache;
            _random = random;
            _skillService = skillService;
            _activityService = activityService;
            _messagingService = messagingService;
        }

        public const string ActiveBaitVariable = "ACTIVE_BAIT";
        public const string RemainingBaitVariable = "REMAINING_BAIT";
        public const string LoadedBaitTypeVariable = "LOADED_BAIT_TYPE";
        private const string FishingPositionVariableX = "FISHING_POSITION_X";
        private const string FishingPositionVariableY = "FISHING_POSITION_Y";
        private const string FishingPositionVariableZ = "FISHING_POSITION_Z";
        private const string FishingPointVariable = "FISHING_POINT";
        private const string FishingPointRemainingAttemptsVariable = "FISHING_POINT_REMAINING_ATTEMPTS";
        private const string FishingPointInitializedVariable = "FISHING_POINT_INITIALIZED";
        private const string FishingAttemptVariable = "FISHING_ATTEMPT_ID";
        public const string FishingPointLocationVariable = "FISHING_LOCATION_ID";
        public const string FishingRodTag = "FISHING_ROD";

        /// <summary>
        /// When the module loads, retrieve and organize all fishing data for quick look-ups.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            LoadFish();
            LoadRods();
            LoadBaits();
            LoadFishingLocations();
        }

        private void LoadFish()
        {
            _fishCache = _cacheService.BuildEnumCache<FishType, FishAttribute>()
                .WithAllItems()
                .Build();

            Console.WriteLine($"Loaded {_fishCache.AllItems.Count} fish.");
        }

        private void LoadRods()
        {
            _rodCache = _cacheService.BuildEnumCache<FishingRodType, FishingRodAttribute>()
                .WithAllItems()
                .Build();

            // Process rods for additional caches
            foreach (var (rodType, rodDetail) in _rodCache.AllItems)
            {
                _rodsByResref[rodDetail.Resref] = rodType;
            }

            Console.WriteLine($"Loaded {_rodCache.AllItems.Count} fishing rods.");
        }

        private void LoadBaits()
        {
            _baitCache = _cacheService.BuildEnumCache<FishingBaitType, FishingBaitAttribute>()
                .WithAllItems()
                .Build();

            // Process baits for additional caches
            foreach (var (baitType, baitDetail) in _baitCache.AllItems)
            {
                foreach (var resref in baitDetail.Resrefs)
                {
                    _baitsByResref[resref] = baitType;
                }
            }

            Console.WriteLine($"Loaded {_baitCache.AllItems.Count} fishing baits.");
        }

        private void LoadFishingLocations()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IFishingLocationDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IFishingLocationDefinition)Activator.CreateInstance(type);
                var fishingLocations = instance.Build();

                foreach (var (locationType, locationDetail) in fishingLocations)
                {
                    if (!_fishingLocations.ContainsKey(locationType))
                    {
                        _fishingLocations[locationType] = locationDetail;
                    }
                    else
                    {
                        var existingLocation = _fishingLocations[locationType];
                        foreach (var (key, list) in locationDetail.FishMap)
                        {
                            if (!existingLocation.FishMap.ContainsKey(key))
                                existingLocation.FishMap[key] = new List<FishDetail>();

                            existingLocation.FishMap[key].AddRange(list);
                        }
                    }

                    if (!_fishResrefsByLocation.ContainsKey(locationType))
                    {
                        _fishResrefsByLocation[locationType] = new List<string>();

                        foreach (var (key, list) in locationDetail.FishMap)
                        {
                            foreach (var fish in list)
                            {
                                var fishDetail = _fishCache.AllItems[fish.Type];
                                if (fishDetail.DisplayInDescription && 
                                    !_fishResrefsByLocation[locationType].Contains(fishDetail.Resref))
                                {
                                    _fishResrefsByLocation[locationType].Add(fishDetail.Resref);
                                }
                            }
                        }

                    }

                }
            }

            Console.WriteLine($"Loaded {_fishingLocations.Count} fishing locations.");
        }

        /// <summary>
        /// Determines if an item is a fishing rod.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if fishing rod, false otherwise</returns>
        public bool IsItemFishingRod(uint item)
        {
            var resref = GetResRef(item);
            return _rodsByResref.ContainsKey(resref);
        }

        /// <summary>
        /// Determines if an item is a bait item.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if bait, false otherwise</returns>
        public bool IsItemBait(uint item)
        {
            var resref = GetResRef(item);
            return _baitsByResref.ContainsKey(resref);
        }

        /// <summary>
        /// Retrieves the type of fishing bait by its resref.
        /// </summary>
        /// <param name="resref">The resref to look for</param>
        /// <returns>The type of bait.</returns>
        public FishingBaitType GetBaitByResref(string resref)
        {
            if (!_baitsByResref.ContainsKey(resref))
                return FishingBaitType.Invalid;

            return _baitsByResref[resref];
        }

        /// <summary>
        /// Retrieves the details about a specific fishing location.
        /// </summary>
        /// <param name="type">The type of fishing location.</param>
        /// <returns>Details about the specified fishing location.</returns>
        public List<string> GetFishAvailableAtLocation(FishingLocationType type)
        {
            return _fishResrefsByLocation[type];
        }

        /// <summary>
        /// Retrieves the type of fishing bait currently loaded on a fishing rod.
        /// </summary>
        /// <param name="rod">The fishing rod item to check</param>
        /// <returns>The loaded bait type.</returns>
        public FishingBaitType GetLoadedBait(uint rod)
        {
            return (FishingBaitType)GetLocalInt(rod, LoadedBaitTypeVariable);
        }

        private void InitializeFishingPoint(uint fishingPoint)
        {
            if (GetLocalBool(fishingPoint, FishingPointInitializedVariable))
                return;

            var attempts = 5 + _random.Next(10);
            SetLocalInt(fishingPoint, FishingPointRemainingAttemptsVariable, attempts);

            SetLocalBool(fishingPoint, FishingPointInitializedVariable, true);
        }

        /// <summary>
        /// Runs when a player interacts with a fishing point.
        /// </summary>
        [ScriptHandler(ScriptName.OnFishPoint)]
        public void ClickFishingPoint()
        {
            ClickFishingPointInternal();
        }

        private void ClickFishingPointInternal()
        {
            void CheckPosition(uint player, Vector3 startPosition, string attemptId)
            {
                var position = GetPosition(player);

                if (attemptId != GetLocalString(player, FishingAttemptVariable))
                    return;

                if (startPosition.X != position.X ||
                    startPosition.Y != position.Y ||
                    startPosition.Z != position.Z)
                {
                    SendMessageToPC(player, "You move and interrupt your cast.");
                    ClearFishingAttempt(player);
                    PlayerPlugin.StopGuiTimingBar(player);
                    return;
                }

                DelayCommand(0.1f, () => CheckPosition(player, startPosition, attemptId));
            }

            var fishingPoint = OBJECT_SELF;
            InitializeFishingPoint(fishingPoint);

            var player = GetPlaceableLastClickedBy();
            AssignCommand(player, () => ClearAllActions());

            var rod = GetItemInSlot(InventorySlot.RightHand, player);
            const float MaxDistance = 10f;

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, "Only players may fish.");
                return;
            }

            if (_activityService.IsBusy(player))
            {
                SendMessageToPC(player, "You are busy.");
                return;
            }

            if (!GetIsObjectValid(rod) ||
                GetTag(rod) != FishingRodTag)
            {
                SendMessageToPC(player, "A fishing rod must be equipped.");
                return;
            }

            var baitType = GetLoadedBait(rod);
            if (baitType == FishingBaitType.Invalid ||
                GetLocalInt(rod, RemainingBaitVariable) <= 0)
            {
                SendMessageToPC(player, "Your fishing rod has no bait loaded. Use the fishing rod and target a bait to load some.");
                return;
            }

            if (GetDistanceBetween(player, fishingPoint) > MaxDistance)
            {
                SendMessageToPC(player, "You are too far away from the fishing point.");
                return;
            }

            if (!GetIsObjectValid(fishingPoint) || GetIsDead(fishingPoint))
            {
                // Clear any existing busy state if the fishing point is exhausted
                _activityService.ClearBusy(player);
                SendMessageToPC(player, "This fishing point has been exhausted.");
                return;
            }

            var attemptId = Guid.NewGuid().ToString();
            var position = GetPosition(player);
            SetLocalFloat(player, FishingPositionVariableX, position.X);
            SetLocalFloat(player, FishingPositionVariableY, position.Y);
            SetLocalFloat(player, FishingPositionVariableZ, position.Z);
            SetLocalString(player, FishingAttemptVariable, attemptId);
            SetLocalObject(player, FishingPointVariable, fishingPoint);

            var fishingDelay = 6 + _random.Next(3);
            PlayerPlugin.StartGuiTimingBar(player, fishingDelay, "finish_fishing");

            _activityService.SetBusy(player, ActivityStatusType.Fishing);
            _messagingService.SendMessageNearbyToPlayers(player, $"{GetName(player)} casts a line into the water.");

            BiowarePosition.TurnToFaceObject(fishingPoint, player);
            CheckPosition(player, position, attemptId);
        }

        /// <summary>
        /// Runs when the fishing process completes.
        /// </summary>
        [ScriptHandler(ScriptName.OnFinishFishing)]
        public void FinishFishing()
        {
            FinishFishingInternal();
        }

        private void FinishFishingInternal()
        {
            var player = OBJECT_SELF;
            var fishingPoint = GetLocalObject(player, FishingPointVariable);
            var startPosition = Vector3(
                GetLocalFloat(player, FishingPositionVariableX),
                GetLocalFloat(player, FishingPositionVariableY),
                GetLocalFloat(player, FishingPositionVariableZ)
            );
            var position = GetPosition(player);
            var locationId = (FishingLocationType)GetLocalInt(fishingPoint, FishingPointLocationVariable);
            var rod = GetItemInSlot(InventorySlot.RightHand, player);
            var rodResref = GetResRef(rod);

            ClearFishingAttempt(player);

            if (startPosition.X != position.X ||
                startPosition.Y != position.Y ||
                startPosition.Z != position.Z)
            {
                SendMessageToPC(player, "You move and interrupt your cast.");
                return;
            }

            if (!GetIsObjectValid(fishingPoint) || GetIsDead(fishingPoint))
            {
                // Clear any existing busy state if the fishing point is exhausted
                _activityService.ClearBusy(player);
                SendMessageToPC(player, "This fishing point has been exhausted.");
                return;
            }

            if (!GetIsObjectValid(rod) ||
                GetTag(rod) != FishingRodTag)
            {
                SendMessageToPC(player, "A fishing rod must be equipped.");
                return;
            }

            var baitType = GetLoadedBait(rod);
            if (baitType == FishingBaitType.Invalid ||
                GetLocalInt(rod, RemainingBaitVariable) <= 0)
            {
                SendMessageToPC(player, "Your fishing rod has no bait loaded. Use the fishing rod and target a bait to load some.");
                return;
            }

            if (locationId == FishingLocationType.Invalid)
            {
                SendMessageToPC(player, "Invalid location Id for this fishing point. Please report this with the /bug chat command.");
                return;
            }

            if (!_fishingLocations.ContainsKey(locationId))
            {
                SendMessageToPC(player, "Valid location Id but no fish are assigned to this point. Please report this with the /bug chat command.");
                return;
            }

            if (!_rodsByResref.ContainsKey(rodResref))
            {
                SendMessageToPC(player, "Fishing rod is not registered in the system. Please report this with the /bug chat command.");
                return;
            }

            var rodType = _rodsByResref[rodResref];
            var baitDetail = _baitCache.AllItems[baitType];
            var baitName = _itemCache.GetItemNameByResref(baitDetail.Resrefs.First());
            var locationDetail = _fishingLocations[locationId];
            var (fishType, isDefaultFish) = locationDetail.GetRandomFish(rodType, baitType);
            var fish = _fishCache.AllItems[fishType];

            // Default fish was picked - 80% to not get a bite.
            if (isDefaultFish)
            {
                if (_random.D100(1) <= 80)
                {
                    SendMessageToPC(player, "Not even a nibble...");
                    return;
                }
            }

            // We're guaranteed to enter the fishing mini-game from here on.
            // Go ahead and decrement the bait because one unit needs to be lost for fish attempt.
            var remainingBait = GetLocalInt(rod, RemainingBaitVariable) - 1;
            if (remainingBait < 0)
                remainingBait = 0;
            SetLocalInt(rod, RemainingBaitVariable, remainingBait);

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var skill = dbPlayer.Skills[SkillType.Agriculture].Rank;

            const int BaseChance = 40;
            var delta = skill - fish.Level;
            var stat = GetAbilityScore(player, AbilityType.Willpower);

            var chance = BaseChance + delta * 10 + stat * 2;
            if (chance > 95)
                chance = 95;
            else if (chance < 0)
                chance = 0;

            if (_random.D100(1) > chance)
            {
                SendMessageToPC(player, ColorToken.Red("You failed to reel the fish in..."));
            }
            else
            {
                CreateItemOnObject(fish.Resref, player);
                SendMessageToPC(player, $"You landed a {fish.Name}!");
            }

            SendMessageToPC(player, $"Bait Remaining: {remainingBait}x {baitName}");

            // Handle fishing point exhaustion
            var remainingAttempts = GetLocalInt(fishingPoint, FishingPointRemainingAttemptsVariable) - 1;
            if (remainingAttempts <= 0)
            {
                // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
                // Use EffectDeath to ensure that we trigger death processing.
                SetPlotFlag(fishingPoint, false);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), fishingPoint);

                _messagingService.SendMessageNearbyToPlayers(fishingPoint, "The fishing point has been exhausted.");
            }
            else
            {
                SetLocalInt(fishingPoint, FishingPointRemainingAttemptsVariable, remainingAttempts);
            }

            var xp = _skillService.GetDeltaXP(fish.Level - skill);
            _skillService.GiveSkillXP(player, SkillType.Agriculture, xp, false, false);
        }

        private void ClearFishingAttempt(uint player)
        {
            Activity.ClearBusy(player);
            DeleteLocalFloat(player, FishingPositionVariableX);
            DeleteLocalFloat(player, FishingPositionVariableY);
            DeleteLocalFloat(player, FishingPositionVariableZ);
            DeleteLocalObject(player, FishingPointVariable);
            DeleteLocalString(player, FishingAttemptVariable);
        }
    }
}
