using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Crafting.Minigame.Line;
using SWLOR.Game.Server.Domain.World;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Fishing: the rod, bait and fish tables and the cast-and-reel flow that used to be a static class, moved here
    /// unchanged. The event handlers run on the container's instance; static callers reach it through
    /// <c>GameServices.Fishing</c>.
    /// </summary>
    public class FishingService : IFishingService
    {
        private readonly Dictionary<FishType, FishAttribute> _fish = new();
        private readonly Dictionary<FishingRodType, FishingRodAttribute> _rods = new();
        private readonly Dictionary<FishingBaitType, FishingBaitAttribute> _baits = new();

        private readonly Dictionary<string, FishingRodType> _rodsByResref = new();
        private readonly Dictionary<string, FishingBaitType> _baitsByResref = new();
        private readonly Dictionary<FishingLocationType, FishingLocationDetail> _fishingLocations = new();
        private readonly Dictionary<FishingLocationType, List<string>> _fishResrefsByLocation = new();

        public const string ActiveBaitVariable = "ACTIVE_BAIT";
        public const string RemainingBaitVariable = "REMAINING_BAIT";
        public const string LoadedBaitTypeVariable = "LOADED_BAIT_TYPE";
        private const string FishingPositionVariableX = "FISHING_POSITION_X";
        private const string FishingPositionVariableY = "FISHING_POSITION_Y";
        private const string FishingPositionVariableZ = "FISHING_POSITION_Z";
        private const string FishingPointVariable = "FISHING_POINT";
        public const string FishingPointRemainingAttemptsVariable = "FISHING_POINT_REMAINING_ATTEMPTS";
        private const string FishingPointInitializedVariable = "FISHING_POINT_INITIALIZED";
        private const string FishingAttemptVariable = "FISHING_ATTEMPT_ID";
        private const string FishingAnimationVariable = "FISHING_ANIMATION_TOKEN";
        public const string FishingPointLocationVariable = "FISHING_LOCATION_ID";
        public const string FishingRodTag = "FISHING_ROD";

        private readonly IDBService _db;
        private readonly IActivityService _activity;
        private readonly ICacheService _cache;
        private readonly IFishingFightsService _fishingFights;
        private readonly IMessagingService _messaging;
        private readonly INamedAnimationService _namedAnimation;
        private readonly IPlayerNameService _playerName;
        private readonly IStatService _stat;

        public FishingService(
            IDBService db,
            IActivityService activity,
            ICacheService cache,
            IFishingFightsService fishingFights,
            IMessagingService messaging,
            INamedAnimationService namedAnimation,
            IPlayerNameService playerName,
            IStatService stat)
        {
            _db = db;
            _activity = activity;
            _cache = cache;
            _fishingFights = fishingFights;
            _messaging = messaging;
            _namedAnimation = namedAnimation;
            _playerName = playerName;
            _stat = stat;
        }

        /// <summary>
        /// When the module loads, retrieve and organize all fishing data for quick look-ups.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            LoadFish();
            LoadRods();
            LoadBaits();
            LoadFishingLocations();
        }

        private void LoadFish()
        {
            var fishes = Enum.GetValues(typeof(FishType)).Cast<FishType>();
            foreach (var fish in fishes)
            {
                var fishDetail = fish.GetAttribute<FishType, FishAttribute>();
                _fish[fish] = fishDetail;
            }

            Console.WriteLine($"Loaded {_fish.Count} fish.");
        }

        private void LoadRods()
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

        private void LoadBaits()
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

        private void LoadFishingLocations()
        {
            var types = GameAssemblies.GetAssemblies()
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

            var attempts = 5 + Random.Next(10);
            SetLocalInt(fishingPoint, FishingPointRemainingAttemptsVariable, attempts);

            SetLocalBool(fishingPoint, FishingPointInitializedVariable, true);
        }

        /// <summary>
        /// Runs when a player interacts with a fishing point.
        /// </summary>
        [NWNEventHandler(ScriptName.OnFishPoint)]
        public void ClickFishingPoint()
        {
            void CheckPosition(uint player, Vector3 startPosition, string attemptId)
            {
                if (!GetIsObjectValid(player) || attemptId != GetLocalString(player, FishingAttemptVariable))
                    return;

                var position = GetPosition(player);
                var equippedRod = GetItemInSlot(InventorySlot.RightHand, player);
                var point = GetLocalObject(player, FishingPointVariable);
                var ownsActivity = _activity.GetBusyType(player) == ActivityStatusType.Fishing;
                if (GetIsDead(player) || GetIsInCombat(player) ||
                    !ownsActivity ||
                    !GetIsObjectValid(equippedRod) || GetTag(equippedRod) != FishingRodTag ||
                    !GetIsObjectValid(point) || GetIsDead(point))
                {
                    ClearFishingAttempt(player);
                    // A replacement activity may already own the player's progress bar.
                    if (ownsActivity)
                        PlayerPlugin.StopGuiTimingBar(player);
                    return;
                }

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

            var rod = GetItemInSlot(InventorySlot.RightHand, player);
            const float MaxDistance = 10f;

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, "Only players may fish.");
                return;
            }

            if (_activity.IsBusy(player))
            {
                SendMessageToPC(player, "You are busy.");
                return;
            }

            if (GetIsDead(player) || GetIsInCombat(player))
            {
                SendMessageToPC(player, "You cannot fish right now.");
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
                _activity.ClearBusy(player);
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
            PlayerPlugin.StartGuiTimingBar(player, fishingDelay);
            // Capture this attempt: an old timer must never finish a restarted cast.
            AssignCommand(GetModule(), () => DelayCommand(fishingDelay, () =>
            {
                if (GetIsObjectValid(player) && GetLocalString(player, FishingAttemptVariable) == attemptId)
                    FinishFishing(player);
            }));

            _activity.SetBusy(player, ActivityStatusType.Fishing);
            _messaging.SendMessageNearbyToPlayers(
                player,
                receiver => $"{_playerName.GetDisplayName(receiver, player)} casts a line into the water.");

            ClearAllActions(oObject: player);
            BiowarePosition.TurnToFaceObject(fishingPoint, player);
            var clip = GetFishingAnimation(fishingDelay);
            SetLocalString(player, FishingAnimationVariable, _namedAnimation.Play(player, clip));
            AssignCommand(GetModule(), () => CheckPosition(player, position, attemptId));
        }

        /// <summary>Selects the complete fishing sequence matching the activity's wait duration.</summary>
        public AnimationClip GetFishingAnimation(int seconds) => seconds switch
        {
            6 => AuthoredAnimation.Fishing6,
            7 => AuthoredAnimation.Fishing7,
            8 => AuthoredAnimation.Fishing8,
            _ => throw new ArgumentOutOfRangeException(nameof(seconds))
        };

        /// <summary>
        /// Runs when the fishing process completes.
        /// </summary>
        private void FinishFishing(uint player)
        {
            if (string.IsNullOrEmpty(GetLocalString(player, FishingAttemptVariable)))
                return;
            if (GetIsDead(player) || GetIsInCombat(player) || _activity.GetBusyType(player) != ActivityStatusType.Fishing)
            {
                ClearFishingAttempt(player);
                return;
            }
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
                _activity.ClearBusy(player);
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
            var baitName = GetBaitName(baitDetail.Resrefs.First());
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

            // We're guaranteed to enter the fishing mini-game from here on. The bite spends one bait, takes one of the
            // fishing point's bites and pays the XP; the fight it opens decides whether the fish is landed.
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var skill = dbPlayer.Skills[SkillType.Agriculture].Rank;

            StartFight(new FishingBite(
                player,
                rod,
                fishingPoint,
                fish,
                rodType,
                skill,
                GetLocalInt(rod, RemainingBaitVariable),
                baitName,
                GetLocalInt(fishingPoint, FishingPointRemainingAttemptsVariable),
                position,
                GetAnglerEffects(player)));
        }

        /// <summary>
        /// The name of a bait item for the "Bait Remaining" message. <c>protected virtual</c> so a unit test can answer it:
        /// the item name cache spawns the item in the engine to learn its name. The body is the original call.
        /// </summary>
        protected virtual string GetBaitName(string resref)
        {
            return _cache.GetItemNameByResref(resref);
        }

        /// <summary>
        /// Opens the fight a bite starts: it spends the bait, takes one of the fishing point's bites and pays the XP.
        /// <c>protected virtual</c> so a unit test can record the bite: the fight runs against the game engine. The body is the
        /// original call.
        /// </summary>
        protected virtual void StartFight(FishingBite bite)
        {
            _fishingFights.Bite(bite);
        }

        /// <summary>
        /// What the angler's perks change in the fight. <c>protected virtual</c> so a unit test can answer it: the stat service
        /// reads the player's stat totals. The body is the original expression.
        /// </summary>
        protected virtual LineAnglerEffects GetAnglerEffects(uint player)
        {
            return LineAnglerEffects.FromStatTotals(stat => _stat.GetStatAdjustment(player, stat));
        }

        /// <summary>Releases this attempt's animation and state without clearing a replacement activity.</summary>
        private void ClearFishingAttempt(uint player)
        {
            _namedAnimation.StopIfCurrent(player, GetLocalString(player, FishingAnimationVariable));
            DeleteLocalString(player, FishingAnimationVariable);
            if (_activity.GetBusyType(player) == ActivityStatusType.Fishing)
                _activity.ClearBusy(player);
            DeleteLocalFloat(player, FishingPositionVariableX);
            DeleteLocalFloat(player, FishingPositionVariableY);
            DeleteLocalFloat(player, FishingPositionVariableZ);
            DeleteLocalObject(player, FishingPointVariable);
            DeleteLocalString(player, FishingAttemptVariable);
        }
    }
}
