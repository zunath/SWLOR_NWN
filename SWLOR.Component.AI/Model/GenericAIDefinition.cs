using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Shared.Domain.Contracts;

namespace SWLOR.Component.AI.Model
{
    public class GenericAIDefinition: AIBase
    {
        public GenericAIDefinition(IAbilityService abilityService, IPerkService perkService, IStatusEffectService statusEffectService) 
            : base(abilityService, perkService, statusEffectService)
        {
        }
    }
}
