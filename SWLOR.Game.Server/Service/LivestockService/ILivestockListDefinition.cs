using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.LivestockService
{
    public interface ILivestockListDefinition
    {
        public Dictionary<LivestockType, LivestockDefinition> BuildLivestock();
    }
}
