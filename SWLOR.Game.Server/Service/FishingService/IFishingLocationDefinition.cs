using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FishingService
{
    public interface IFishingLocationDefinition
    {
        public Dictionary<FishingLocationType, FishingLocationDetail> Build();
    }
}
