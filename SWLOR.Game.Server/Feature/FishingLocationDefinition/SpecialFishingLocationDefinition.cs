using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class SpecialFishingLocationDefinition: IFishingLocationDefinition
    {
        private readonly IRandomService _random;
        private readonly IFishingLocationBuilder _builder = new();

        public SpecialFishingLocationDefinition(IRandomService random)
        {
            _random = random;
        }

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            SpecialFish();

            return _builder.Build();
        }

        private void SpecialFish()
        {
            CreateRandomSpawn(FishType.Mercanbaligi);
            CreateRandomSpawn(FishType.Nashmau);
            CreateRandomSpawn(FishType.Rhinochimera);
            CreateRandomSpawn(FishType.Mhaura);
            CreateRandomSpawn(FishType.Zitah);
            CreateRandomSpawn(FishType.Alzabi);
        }

        private void CreateRandomSpawn(FishType fishType)
        {
            var locations = Enum.GetValues<FishingLocationType>().ToList();
            locations.Remove(FishingLocationType.Invalid);

            var locationIndex = _random.Next(locations.Count);
            var location = locations[locationIndex];

            var baits = Enum.GetValues<FishingBaitType>().ToList();
            baits.Remove(FishingBaitType.Invalid);
            var baitIndex = _random.Next(baits.Count);
            var bait = baits[baitIndex];

            _builder
                .Create(location)

                .AddFish(fishType, FishingRodType.LuShang, bait);
        }

    }
}
