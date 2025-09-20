
using System.Collections.Generic;
using System;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Service
{
    public static class Fishing
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private static readonly Dictionary<FishType, FishAttribute> _fish = new();
        private static readonly Dictionary<FishingRodType, FishingRodAttribute> _rods = new();
        private static readonly Dictionary<FishingBaitType, FishingBaitAttribute> _baits = new();

        private static readonly Dictionary<string, FishingRodType> _rodsByResref = new();
        private static readonly Dictionary<string, FishingBaitType> _baitsByResref = new();
        private static readonly Dictionary<FishingLocationType, FishingLocationDetail> _fishingLocations = new();
        private static readonly Dictionary<FishingLocationType, List<string>> _fishResrefsByLocation = new();

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
        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            LoadFish();
            LoadRods();
            LoadBaits();
            LoadFishingLocations();
        }

        private static void LoadFish()
        {
            var fishes = Enum.GetValues(typeof(FishType)).Cast<FishType>();
            foreach (var fish in fishes)
            {
                var fishDetail = fish.GetAttribute<FishType, FishAttribute>();
                _fish[fish] = fishDetail;
            }

            Console.WriteLine($"Loaded {_fish.Count} fish.");
        }

        private static void LoadRods()
        {
            var rods = Enum.GetValues(typeof(FishingRodType)).Cast<FishingRodType>();
            foreach (var rod in rods)
            {
                var rodDetail = rod.GetAttribute<FishingRodType, FishingRodAttribute>();
                _rods[rod] = rodDetail;

                _rodsByResref[rodDetail.Resref] = rod;
            }

            Console.WriteLine($"Loaded {_rods.Count} fishing rods.");
        }

        private static void LoadBaits()
        {
            var baits = Enum.GetValues(typeof(FishingBaitType)).Cast<FishingBaitType>();
            foreach (var bait in baits)
            {
                var baitDetail = bait.GetAttribute<FishingBaitType, FishingBaitAttribute>();
                _baits[bait] = baitDetail;

                foreach (var resref in baitDetail.Resrefs)
                {
                    _baitsByResref[resref] = bait;
                }
            }

            Console.WriteLine($"Loaded {_baits.Count} fishing baits.");
        }

        private static void LoadFishingLocations()
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
                                var fishDetail = _fish[fish.Type];
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
        public static bool IsItemFishingRod(uint item)
        {
            var resref = GetResRef(item);
            return _rodsByResref.ContainsKey(resref);
        }

        /// <summary>
        /// Determines if an item is a bait item.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if bait, false otherwise</returns>
        public static bool IsItemBait(uint item)
        {
            var resref = GetResRef(item);
            return _baitsByResref.ContainsKey(resref);
        }

        /// <summary>
        /// Retrieves the type of fishing bait by its resref.
        /// </summary>
        /// <param name="resref">The resref to look for</param>
        /// <returns>The type of bait.</returns>
        public static FishingBaitType GetBaitByResref(string resref)
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
        public static List<string> GetFishAvailableAtLocation(FishingLocationType type)
        {
            return _fishResrefsByLocation[type];
        }

        /// <summary>
        /// Retrieves the type of fishing bait currently loaded on a fishing rod.
        /// </summary>
        /// <param name="rod">The fishing rod item to check</param>
        /// <returns>The loaded bait type.</returns>
        public static FishingBaitType GetLoadedBait(uint rod)
        {
            return (FishingBaitType)GetLocalInt(rod, LoadedBaitTypeVariable);
        }

        private static void InitializeFishingPoint(uint fishingPoint)
        {
            if (GetLocalBool(fishingPoint, FishingPointInitializedVariable))
                return;

            var attempts = 5 + Random.Next(10);
            SetLocalInt(fishingPoint, FishingPointRemainingAttemptsVariable, attempts);

            SetLocalBool(fishingPoint, FishingPointInitializedVariable, true);
        }

        /// <summary>
        /// Runs when a player interacts with a fishing point.
        /// </summary>
        [ScriptHandler(ScriptName.OnFishPoint)]
        public static void ClickFishingPoint()
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

            if (Activity.IsBusy(player))
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
                Activity.ClearBusy(player);
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

            var fishingDelay = 6 + Random.Next(3);
            PlayerPlugin.StartGuiTimingBar(player, fishingDelay, "finish_fishing");

            Activity.SetBusy(player, ActivityStatusType.Fishing);
            Messaging.SendMessageNearbyToPlayers(player, $"{GetName(player)} casts a line into the water.");

            BiowarePosition.TurnToFaceObject(fishingPoint, player);
            CheckPosition(player, position, attemptId);
        }

        /// <summary>
        /// Runs when the fishing process completes.
        /// </summary>
        [ScriptHandler(ScriptName.OnFinishFishing)]
        public static void FinishFishing()
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
                Activity.ClearBusy(player);
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
            var baitDetail = _baits[baitType];
            var baitName = Cache.GetItemNameByResref(baitDetail.Resrefs.First());
            var locationDetail = _fishingLocations[locationId];
            var (fishType, isDefaultFish) = locationDetail.GetRandomFish(rodType, baitType);
            var fish = _fish[fishType];

            // Default fish was picked - 80% to not get a bite.
            if (isDefaultFish)
            {
                if (Random.D100(1) <= 80)
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

            if (Random.D100(1) > chance)
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

                Messaging.SendMessageNearbyToPlayers(fishingPoint, "The fishing point has been exhausted.");
            }
            else
            {
                SetLocalInt(fishingPoint, FishingPointRemainingAttemptsVariable, remainingAttempts);
            }

            var xp = Skill.GetDeltaXP(fish.Level - skill);
            Skill.GiveSkillXP(player, SkillType.Agriculture, xp, false, false);
        }

        private static void ClearFishingAttempt(uint player)
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
