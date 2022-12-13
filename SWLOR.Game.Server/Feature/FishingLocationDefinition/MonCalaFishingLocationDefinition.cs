using System.Collections.Generic;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class MonCalaFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            MonCalaCoralIslesInnerFish();
            MonCalaCoralIslesOuterFish();
            MonCalaDacCitySurfaceFish();
            MonCalaSharptoothJungleSouthFish();
            MonCalaSharptoothJungleCavesFish();
            MonCalaSunkenhedgeSwampsFish();

            return _builder.Build();
        }

        private void MonCalaCoralIslesInnerFish()
        {
        }

        private void MonCalaCoralIslesOuterFish()
        {
        }

        private void MonCalaDacCitySurfaceFish()
        {
        }

        private void MonCalaSharptoothJungleSouthFish()
        {
        }

        private void MonCalaSharptoothJungleCavesFish()
        {
        }

        private void MonCalaSunkenhedgeSwampsFish()
        {
        }

    }
}