using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public interface IBeastListDefinition
    {
        public Dictionary<BeastType, BeastDetail> Build();
    }
}
