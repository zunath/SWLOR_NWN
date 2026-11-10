using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.World
{
    public interface IFishingLocationDefinition
    {
        public Dictionary<FishingLocationType, FishingLocationDetail> Build();
    }
}
