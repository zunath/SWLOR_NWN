using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Service
{
    public static class Fishing
    {
        private static readonly Dictionary<FishType, FishAttribute> _fish = new();
        private static readonly Dictionary<FishingRodType, FishingRodAttribute> _rods = new();
        private static readonly Dictionary<FishingBaitType, FishingBaitAttribute> _baits = new();

        private static readonly Dictionary<string, FishingRodType> _rodsByResref = new();
        private static readonly Dictionary<string, FishingBaitType> _baitsByResref = new();
        private static Dictionary<FishingLocationType, FishingLocationDetail> _fishingLocations = new();

        public const string ActiveBaitVariable = "ACTIVE_BAIT";
        public const string RemainingBaitVariable = "REMAINING_BAIT";
        public const string LoadedBaitTypeVariable = "LOADED_BAIT_TYPE";
        private const string FishingPositionVariableX = "FISHING_POSITION_X";
        private const string FishingPositionVariableY = "FISHING_POSITION_Y";
        private const string FishingPositionVariableZ = "FISHING_POSITION_Z";
        private const string FishingPointVariable = "FISHING_POINT";
        private const string FishingAttemptVariable = "FISHING_ATTEMPT_ID";
        private const string FishingPointLocationVariable = "FISHING_LOCATION_ID";
        public const string FishingRodTag = "FISHING_ROD";

        /// <summary>
        /// When the module loads, retrieve and organize all fishing data for quick look-ups.
        /// </summary>
        [NWNEventHandler("mod_cache")]
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
        }

        private static void LoadBaits()
        {
            var baits = Enum.GetValues(typeof(FishingBaitType)).Cast<FishingBaitType>();
            foreach (var bait in baits)
            {
                var baitDetail = bait.GetAttribute<FishingBaitType, FishingBaitAttribute>();
                _baits[bait] = baitDetail;

                _baitsByResref[baitDetail.Resref] = bait;
            }
        }

        private static void LoadFishingLocations()
        {
            var definition = new FishingLocationDefinition();
            _fishingLocations = definition.Create();
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
        /// Retrieves the type of fishing bait currently loaded on a fishing rod.
        /// </summary>
        /// <param name="rod">The fishing rod item to check</param>
        /// <returns>The loaded bait type.</returns>
        public static FishingBaitType GetLoadedBait(uint rod)
        {
            return (FishingBaitType)GetLocalInt(rod, LoadedBaitTypeVariable);
        }

        /// <summary>
        /// Runs when a player interacts with a fishing point.
        /// </summary>
        [NWNEventHandler("fish_point")]
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

            var player = GetPlaceableLastClickedBy();
            AssignCommand(player, () => ClearAllActions());

            var rod = GetItemInSlot(InventorySlot.RightHand, player);
            var fishingPoint = OBJECT_SELF;
            const float MaxDistance = 10f;

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
            if (baitType == FishingBaitType.Invalid)
            {
                SendMessageToPC(player, "Your fishing rod has no bait loaded. Use the fishing rod and target a bait to load some.");
                return;
            }

            if (GetDistanceBetween(player, fishingPoint) > MaxDistance)
            {
                SendMessageToPC(player, "You are too far away from the fishing point.");
                return;
            }

            var attemptId = Guid.NewGuid().ToString();
            var position = GetPosition(player);
            SetLocalFloat(player, FishingPositionVariableX, position.X);
            SetLocalFloat(player, FishingPositionVariableY, position.Y);
            SetLocalFloat(player, FishingPositionVariableZ, position.Z);
            SetLocalString(player, FishingAttemptVariable, attemptId);

            PlayerPlugin.StartGuiTimingBar(player, 6f, "finish_fishing");

            Activity.SetBusy(player, ActivityStatusType.Fishing);
            Messaging.SendMessageNearbyToPlayers(player, $"{GetName(player)} casts a line into the water.");

            BiowarePosition.TurnToFaceObject(fishingPoint, player);
            CheckPosition(player, position, attemptId);
        }

        /// <summary>
        /// Runs when the fishing process completes.
        /// </summary>
        [NWNEventHandler("finish_fishing")]
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

            if (!GetIsObjectValid(fishingPoint))
            {
                SendMessageToPC(player, "This fishing point has been exhausted.");
                return;
            }

            if (!GetIsObjectValid(rod) ||
                GetTag(rod) != FishingRodTag)
            {
                SendMessageToPC(player, "A fishing rod must be equipped.");
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
            var baitType = GetLoadedBait(rod);
            var locationDetail = _fishingLocations[locationId];
            var fishType = locationDetail.GetRandomFish(rodType, baitType);
            var fish = _fish[fishType];

            // todo: determine chance to pull in fish based on fish level vs skill level

            CreateItemOnObject(fish.Resref, player);
            SendMessageToPC(player, $"You landed a {fish.Name}!");

            // todo: determine fishing location exhaustion
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
