using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Shared.Domain.Contracts;

namespace SWLOR.Component.AI.Model
{
    public class DroidAIDefinition: AIBase
    {
        public DroidAIDefinition(IAbilityService abilityService, IPerkService perkService, IStatusEffectService statusEffectService) 
            : base(abilityService, perkService, statusEffectService)
        {
        }
    }
}
