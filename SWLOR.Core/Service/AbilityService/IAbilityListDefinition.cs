using SWLOR.Core.NWScript.Enum;

namespace SWLOR.Core.Service.AbilityService
{
    public interface IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities();
    }
}
