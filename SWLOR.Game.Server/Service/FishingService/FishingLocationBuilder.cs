using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FishingService
{
    public class FishingLocationBuilder
    {
        private readonly Dictionary<FishingLocationType, FishingLocationDetail> _locations = new();
        private FishingLocationDetail _activeDetail;
        private FishDetail _activeFishDetail;

        /// <summary>
        /// Creates a new fishing location. This should be called first.
        /// </summary>
        /// <param name="type">The fishing location type.</param>
        /// <returns>A configured FishingLocationBuilder</returns>
        public FishingLocationBuilder Create(FishingLocationType type)
        {
            if (_locations.ContainsKey(type))
                _activeDetail = _locations[type];
            else
            {
                _activeDetail = new FishingLocationDetail();
                _locations.Add(type, _activeDetail);
            }

            return this;
        }
        /// <summary>
        /// In the event no fish can be retrieved from the spawn table, this fish
        /// will be used as the fallback. If this is unspecified, Moat Carp is the default.
        /// </summary>
        /// <param name="defaultFish">The type of fish to use</param>
        /// <returns>A configured FishingLocationBuilder</returns>
        public FishingLocationBuilder DefaultFish(FishType defaultFish)
        {
            _activeDetail.SetDefaultFish(defaultFish);

            return this;
        }

        /// <summary>
        /// Adds a new fish for a rod/bait configuration. This fish will only appear
        /// if both the rod and bait used matches. 
        /// </summary>
        /// <param name="fishType">The type of fish to use</param>
        /// <param name="rodType">The type of rod to use</param>
        /// <param name="baitType">The type of bait to use</param>
        /// <returns>A configured FishingLocationBuilder</returns>
        public FishingLocationBuilder AddFish(
            FishType fishType, 
            FishingRodType rodType, 
            FishingBaitType baitType)
        {
            _activeFishDetail = new FishDetail
            {
                Type = fishType,
                Frequency = 1,
                TimeOfDay = FishTimeOfDayType.All
            };

            _activeDetail.AddFish(rodType, baitType, _activeFishDetail);

            return this;
        }

        /// <summary>
        /// Indicates the weighted frequency this fish will appear, as compared to all
        /// other weights in the same rod/bait spawn table.
        /// </summary>
        /// <param name="frequency">The weighted frequency this fish will appear.</param>
        /// <returns>A configured FishingLocationBuilder</returns>
        public FishingLocationBuilder Frequency(int frequency)
        {
            _activeFishDetail.Frequency = frequency;

            return this;
        }

        /// <summary>
        /// Indicates this fish will only appear during daytime.
        /// </summary>
        /// <returns>A configured FishingLocationBuilder</returns>
        public FishingLocationBuilder DaytimeOnly()
        {
            _activeFishDetail.TimeOfDay = FishTimeOfDayType.Daytime;

            return this;
        }

        /// <summary>
        /// Indicates this fish will only appear during nighttime.
        /// </summary>
        /// <returns>A configured FishingLocationBuilder</returns>
        public FishingLocationBuilder NighttimeOnly()
        {
            _activeFishDetail.TimeOfDay = FishTimeOfDayType.Nighttime;

            return this;
        }

        /// <summary>
        /// Builds a dictionary of fishing locations
        /// which is stored in cache for quick reference by the fishing system.
        /// </summary>
        /// <returns>A dictionary of configured fishing locations.</returns>
        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            return _locations;
        }
    }
}
