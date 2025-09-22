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
