using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Contracts
{
    public interface IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder);
    }
}
