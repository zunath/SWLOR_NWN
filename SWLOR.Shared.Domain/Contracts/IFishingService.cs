using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Contracts
{
    public interface IFishingService
    {
        /// <summary>
        /// When the module loads, retrieve and organize all fishing data for quick look-ups.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Determines if an item is a fishing rod.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if fishing rod, false otherwise</returns>
        bool IsItemFishingRod(uint item);

        /// <summary>
        /// Determines if an item is a bait item.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if bait, false otherwise</returns>
        bool IsItemBait(uint item);

        /// <summary>
        /// Retrieves the type of fishing bait by its resref.
        /// </summary>
        /// <param name="resref">The resref to look for</param>
        /// <returns>The type of bait.</returns>
        FishingBaitType GetBaitByResref(string resref);

        /// <summary>
        /// Retrieves the details about a specific fishing location.
        /// </summary>
        /// <param name="type">The type of fishing location.</param>
        /// <returns>Details about the specified fishing location.</returns>
        List<string> GetFishAvailableAtLocation(FishingLocationType type);

        /// <summary>
        /// Retrieves the type of fishing bait currently loaded on a fishing rod.
        /// </summary>
        /// <param name="rod">The fishing rod item to check</param>
        /// <returns>The loaded bait type.</returns>
        FishingBaitType GetLoadedBait(uint rod);

        /// <summary>
        /// Runs when a player interacts with a fishing point.
        /// </summary>
        void ClickFishingPoint();

        /// <summary>
        /// Runs when the fishing process completes.
        /// </summary>
        void FinishFishing();
    }
}