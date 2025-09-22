using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Component.Ability.Contracts
{
    public interface IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder);
    }
}
