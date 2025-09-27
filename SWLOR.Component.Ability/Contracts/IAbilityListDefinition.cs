using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.ValueObjects;

namespace SWLOR.Component.Ability.Contracts
{
    public interface IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder);
    }
}
