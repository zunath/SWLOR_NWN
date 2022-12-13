using SWLOR.Game.Server.Service.FishingService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class TatooineFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            TatooineBabySarlaccCaveFish();
            TatooineTuskenRaiderCaveMainFloorFish();

            return _builder.Build();
        }

        private void TatooineBabySarlaccCaveFish()
        {
        }

        private void TatooineTuskenRaiderCaveMainFloorFish()
        {
        }
    }
}
