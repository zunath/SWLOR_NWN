using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks();
    }
}
