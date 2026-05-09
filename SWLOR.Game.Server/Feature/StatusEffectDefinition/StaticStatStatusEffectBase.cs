using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class StaticStatStatusEffectBase : StatusEffectBase
    {
        protected StaticStatStatusEffectBase(StatType stat, int amount)
        {
            StatGroup.Stats[stat] = amount;
        }
    }
}
