using SWLOR.Game.Server.Domain.Character;

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
