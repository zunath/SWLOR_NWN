using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FarmingService
{
    public interface ICropListDefinition
    {
        Dictionary<CropType, CropDetail> BuildCrops();
    }
}
