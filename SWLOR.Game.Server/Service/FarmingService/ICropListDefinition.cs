using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FarmingService
{
    public interface ICropListDefinition
    {
        public Dictionary<CropType, CropDefinition> BuildCrops();
    }
}
