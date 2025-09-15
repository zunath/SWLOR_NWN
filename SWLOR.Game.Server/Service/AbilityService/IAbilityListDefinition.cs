using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public interface IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities();
    }
}
