using System.Collections.Generic;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IFishingLocationBuilder
    {
        /// <summary>
        /// Creates a new fishing location. This should be called first.
        /// </summary>
        /// <param name="type">The fishing location type.</param>
        /// <returns>A configured FishingLocationBuilder</returns>
        IFishingLocationBuilder Create(FishingLocationType type);

        /// <summary>
        /// In the event no fish can be retrieved from the spawn table, this fish
        /// will be used as the fallback. If this is unspecified, Moat Carp is the default.
        /// </summary>
        /// <param name="defaultFish">The type of fish to use</param>
        /// <returns>A configured FishingLocationBuilder</returns>
        IFishingLocationBuilder DefaultFish(FishType defaultFish);

        /// <summary>
        /// Adds a new fish for a rod/bait configuration. This fish will only appear
        /// if both the rod and bait used matches. 
        /// </summary>
        /// <param name="fishType">The type of fish to use</param>
        /// <param name="rodType">The type of rod to use</param>
        /// <param name="baitType">The type of bait to use</param>
        /// <returns>A configured FishingLocationBuilder</returns>
        IFishingLocationBuilder AddFish(
            FishType fishType, 
            FishingRodType rodType, 
            FishingBaitType baitType);

        /// <summary>
        /// Indicates the weighted frequency this fish will appear, as compared to all
        /// other weights in the same rod/bait spawn table.
        /// </summary>
        /// <param name="frequency">The weighted frequency this fish will appear.</param>
        /// <returns>A configured FishingLocationBuilder</returns>
        IFishingLocationBuilder Frequency(int frequency);

        /// <summary>
        /// Indicates this fish will only appear during daytime.
        /// </summary>
        /// <returns>A configured FishingLocationBuilder</returns>
        IFishingLocationBuilder DaytimeOnly();

        /// <summary>
        /// Indicates this fish will only appear during nighttime.
        /// </summary>
        /// <returns>A configured FishingLocationBuilder</returns>
        IFishingLocationBuilder NighttimeOnly();

        /// <summary>
        /// Builds a dictionary of fishing locations
        /// which is stored in cache for quick reference by the fishing system.
        /// </summary>
        /// <returns>A dictionary of configured fishing locations.</returns>
        Dictionary<FishingLocationType, FishingLocationDetail> Build();
    }
}
