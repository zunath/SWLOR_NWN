using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerkService
{
    public interface IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks();
    }
}
