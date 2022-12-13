using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.FishingService
{
    public class FishingLocationDetail
    {
        private readonly Dictionary<Tuple<FishingRodType, FishingBaitType>, List<FishDetail>> _fishMap = new();

        /// <summary>
        /// Retrieves the resref of a random fish given a rod and bait type.
        /// If no fish can be found, FishType.Invalid will be returned.
        /// </summary>
        /// <param name="rodType">The type of rod being used.</param>
        /// <param name="baitType">The type of bait being used.</param>
        /// <returns>The type of fish retrieved.</returns>
        public FishType GetRandomFish(FishingRodType rodType, FishingBaitType baitType)
        {
            var key = new Tuple<FishingRodType, FishingBaitType>(rodType, baitType);
            if (!_fishMap.ContainsKey(key))
                return FishType.Invalid;

            var availableFish = _fishMap[key];
            var weights = availableFish.Select(s => s.Weight).ToArray();
            var selectedIndex = Random.GetRandomWeightedIndex(weights);
            var selectedFish = availableFish[selectedIndex];

            return selectedFish.Type;
        }

        /// <summary>
        /// Adds a new fish to this location for a given rod and bait type.
        /// </summary>
        /// <param name="rodType">The type of rod to associate with this fish.</param>
        /// <param name="baitType">The type of bait to associate with this fish.</param>
        /// <param name="fishType">The type of fish to associate.</param>
        /// <param name="weight">The weighted chance of this fish appearing when this rod and bait type are used.</param>
        public void AddFish(
            FishingRodType rodType, 
            FishingBaitType baitType, 
            FishType fishType,
            int weight)
        {
            var key = new Tuple<FishingRodType, FishingBaitType>(rodType, baitType);

            if (!_fishMap.ContainsKey(key))
                _fishMap[key] = new List<FishDetail>();

            _fishMap[key].Add(new FishDetail
            {
                Type = fishType,
                Weight = weight
            });
        }

    }
}
