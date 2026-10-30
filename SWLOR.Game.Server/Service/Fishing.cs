using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of fishing. The logic and state live in <see cref="IFishingService"/>; every member here forwards
    /// to the container's instance, and the event handlers stay here so their names in the handler order manifest do
    /// not change.
    /// </summary>
    public static class Fishing
    {
        private static IFishingService Instance => FacadeAccessor.Resolve<IFishingService>();

        public const string ActiveBaitVariable = "ACTIVE_BAIT";
        public const string RemainingBaitVariable = Domain.World.FishingService.RemainingBaitVariable;
        public const string LoadedBaitTypeVariable = Domain.World.FishingService.LoadedBaitTypeVariable;
        public const string FishingPointLocationVariable = Domain.World.FishingService.FishingPointLocationVariable;
        public const string FishingRodTag = Domain.World.FishingService.FishingRodTag;

        /// <summary>
        /// When the module loads, retrieve and organize all fishing data for quick look-ups.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Determines if an item is a fishing rod.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if fishing rod, false otherwise</returns>
        public static bool IsItemFishingRod(uint item)
        {
            return Instance.IsItemFishingRod(item);
        }

        /// <summary>
        /// Determines if an item is a bait item.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if bait, false otherwise</returns>
        public static bool IsItemBait(uint item)
        {
            return Instance.IsItemBait(item);
        }

        /// <summary>
        /// Retrieves the type of fishing bait by its resref.
        /// </summary>
        /// <param name="resref">The resref to look for</param>
        /// <returns>The type of bait.</returns>
        public static FishingBaitType GetBaitByResref(string resref)
        {
            return Instance.GetBaitByResref(resref);
        }

        /// <summary>
        /// Retrieves the details about a specific fishing location.
        /// </summary>
        /// <param name="type">The type of fishing location.</param>
        /// <returns>Details about the specified fishing location.</returns>
        public static List<string> GetFishAvailableAtLocation(FishingLocationType type)
        {
            return Instance.GetFishAvailableAtLocation(type);
        }

        /// <summary>
        /// Retrieves the type of fishing bait currently loaded on a fishing rod.
        /// </summary>
        /// <param name="rod">The fishing rod item to check</param>
        /// <returns>The loaded bait type.</returns>
        public static FishingBaitType GetLoadedBait(uint rod)
        {
            return Instance.GetLoadedBait(rod);
        }

        /// <summary>
        /// Runs when a player interacts with a fishing point.
        /// </summary>
        [NWNEventHandler(ScriptName.OnFishPoint)]
        public static void ClickFishingPoint()
        {
            Instance.ClickFishingPoint();
        }

        /// <summary>Selects the complete fishing sequence matching the activity's wait duration.</summary>
        public static AnimationClip GetFishingAnimation(int seconds)
        {
            return Instance.GetFishingAnimation(seconds);
        }
    }
}
