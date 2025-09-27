using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Enums;
using SWLOR.Component.Crafting.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;

namespace SWLOR.Component.Crafting.Feature.FishingLocationDefinition
{
    public class SpecialFishingLocationDefinition: IFishingLocationDefinition
    {
        private readonly IRandomService _random;
        private readonly IFishingLocationBuilder _builder;

        public SpecialFishingLocationDefinition(IRandomService random, IFishingLocationBuilder builder)
        {
            _random = random;
            _builder = builder;
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
