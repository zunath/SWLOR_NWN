using SWLOR.Game.Server.Service.FishingService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class HutlarFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            HutlarCloningTestSiteFish();
            HutlarQionFoothillsFish();
            HutlarQionTundraFish();
            HutlarQionValleyFish();

            return _builder.Build();
        }

        private void HutlarCloningTestSiteFish()
        {
        }

        private void HutlarQionFoothillsFish()
        {
        }

        private void HutlarQionTundraFish()
        {
        }

        private void HutlarQionValleyFish()
        {
        }

    }
}
