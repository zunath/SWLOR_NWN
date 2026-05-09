using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class StaticAbilityStatusEffectBase : StatusEffectBase
    {
        protected StaticAbilityStatusEffectBase(AbilityType ability, int amount)
        {
            StatGroup.Abilities[ability] = amount;
        }
    }
}
