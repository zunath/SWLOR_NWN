using System.Collections.Generic;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class ViscaraFishingLocationDefinition: IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            ViscaraCavernFish();
            ViscaraDeepwoodsFish();
            ViscaraEasternSwamplandsFish();
            ViscaraLakeFish();
            ViscaraLakeGroundsFish();
            ViscaraMountainValleyFish();
            ViscaraWesternSwamplandsFish();
            ViscaraWildlandsFish();
            ViscaraWildwoodsFish();


            return _builder.Build();
        }

        private void ViscaraCavernFish()
        {

        }

        private void ViscaraDeepwoodsFish()
        {

        }

        private void ViscaraEasternSwamplandsFish()
        {
        }

        private void ViscaraLakeFish()
        {
        }

        private void ViscaraLakeGroundsFish()
        {
        }

        private void ViscaraMountainValleyFish()
        {
        }

        private void ViscaraWesternSwamplandsFish()
        {
        }

        private void ViscaraWildlandsFish()
        {
        }

        private void ViscaraWildwoodsFish()
        {
        }

    }
}
