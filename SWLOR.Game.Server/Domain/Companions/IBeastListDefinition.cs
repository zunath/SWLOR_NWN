using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Companions
{
    public interface IBeastListDefinition
    {
        public Dictionary<BeastType, BeastDetail> Build();
    }
}
