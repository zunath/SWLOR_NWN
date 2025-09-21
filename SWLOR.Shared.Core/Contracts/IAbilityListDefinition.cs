using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Models;
using SWLOR.Game.Server.Service.AbilityServicex;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder);
    }
}
