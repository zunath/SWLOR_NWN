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
