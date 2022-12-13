using SWLOR.Game.Server.Service.FishingService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class DathomirFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            DathomirDesertWestSideFish();
            DathomirGrottoCavernsFish();
            DathomirGrottosFish();
            DathomirMountainsFish();
            DathomirTribeVillageFish();

            return _builder.Build();
        }

        private void DathomirDesertWestSideFish()
        {
        }

        private void DathomirGrottoCavernsFish()
        {
        }

        private void DathomirGrottosFish()
        {
        }

        private void DathomirMountainsFish()
        {
        }

        private void DathomirTribeVillageFish()
        {
        }

    }
}
