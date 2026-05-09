using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceHeal1StatusEffect : ForceHealStatusEffectBase
    {
        public override string Name => "Force Heal I";
        protected override int Amount => 10;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceHeal2StatusEffect),
            typeof(ForceHeal3StatusEffect),
            typeof(ForceHeal4StatusEffect),
            typeof(ForceHeal5StatusEffect)
        };
    }
}
