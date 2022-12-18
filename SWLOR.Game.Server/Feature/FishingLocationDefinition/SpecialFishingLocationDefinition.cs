using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.FishingService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class SpecialFishingLocationDefinition: IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

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

            var locationIndex = Random.Next(locations.Count);
            var location = locations[locationIndex];

            var baits = Enum.GetValues<FishingBaitType>().ToList();
            baits.Remove(FishingBaitType.Invalid);
            var baitIndex = Random.Next(baits.Count);
            var bait = baits[baitIndex];

            _builder
                .Create(location)

                .AddFish(fishType, FishingRodType.LuShang, bait);
        }

    }
}
