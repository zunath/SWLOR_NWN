using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public class DroidAIDefinition: AIBase
    {
        public DroidAIDefinition(IAbilityService abilityService, IPerkService perkService, IStatusEffectService statusEffectService) 
            : base(abilityService, perkService, statusEffectService)
        {
        }
    }
}
