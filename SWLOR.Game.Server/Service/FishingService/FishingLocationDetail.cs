using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.FishingService
{
    public class FishingLocationDetail
    {
        private readonly Dictionary<Tuple<FishingRodType, FishingBaitType>, List<FishDetail>> _fishMap = new();
        private FishType _defaultFish;

        public FishingLocationDetail()
        {
            _defaultFish = FishType.MoatCarp;
        }

        /// <summary>
        /// Retrieves the resref of a random fish given a rod and bait type.
        /// If no fish can be found, the default fish will be returned.
        /// </summary>
        /// <param name="rodType">The type of rod being used.</param>
        /// <param name="baitType">The type of bait being used.</param>
        /// <returns>The type of fish retrieved and whether or not if it was a default fish.</returns>
        public (FishType, bool) GetRandomFish(FishingRodType rodType, FishingBaitType baitType)
        {
            var key = new Tuple<FishingRodType, FishingBaitType>(rodType, baitType);
            if (!_fishMap.ContainsKey(key))
                return (_defaultFish, true);

            var availableFish = _fishMap[key];

            if (GetIsNight())
            {
                availableFish = availableFish
                    .Where(x => x.TimeOfDay == FishTimeOfDayType.All ||
                                x.TimeOfDay == FishTimeOfDayType.Nighttime)
                    .ToList();
            }
            else if (GetIsDay())
            {
                availableFish = availableFish
                    .Where(x => x.TimeOfDay == FishTimeOfDayType.All ||
                                x.TimeOfDay == FishTimeOfDayType.Daytime)
                    .ToList();
            }

            if (availableFish.Count <= 0)
                return (_defaultFish, true);

            var weights = availableFish
                .Select(s => s.Frequency).ToArray();
            var selectedIndex = Random.GetRandomWeightedIndex(weights);
            var selectedFish = availableFish[selectedIndex];

            return (selectedFish.Type, false);
        }

        /// <summary>
        /// Adds a new fish to this location for a given rod and bait type.
        /// </summary>
        /// <param name="rodType">The type of rod to associate with this fish.</param>
        /// <param name="baitType">The type of bait to associate with this fish.</param>
        /// <param name="fish">The fish detail to add to the list.</param>
        public void AddFish(
            FishingRodType rodType, 
            FishingBaitType baitType, 
            FishDetail fish)
        {
            var key = new Tuple<FishingRodType, FishingBaitType>(rodType, baitType);

            if (!_fishMap.ContainsKey(key))
                _fishMap[key] = new List<FishDetail>();

            _fishMap[key].Add(fish);
        }

        /// <summary>
        /// Sets the default fish type for this location. There is a small chance
        /// to catch this default fish even if the user's rod/bait combination doesn't match any other fish criteria.
        /// </summary>
        /// <param name="defaultFishType">The type of fish which will be treated as the default</param>
        public void SetDefaultFish(FishType defaultFishType)
        {
            _defaultFish = defaultFishType;
        }

    }
}
