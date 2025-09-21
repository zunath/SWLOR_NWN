using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public class GenericAIDefinition: AIBase
    {
        public GenericAIDefinition(IAbilityService abilityService, IPerkService perkService, IStatusEffectService statusEffectService) 
            : base(abilityService, perkService, statusEffectService)
        {
        }
    }
}
