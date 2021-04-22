using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public interface IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities();
    }
}
