using System.Collections.Generic;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Service.FishingService
{
    public interface IFishingLocationDefinition
    {
        public Dictionary<FishingLocationType, FishingLocationDetail> Build();
    }
}
