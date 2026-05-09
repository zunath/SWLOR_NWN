using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceHeal2StatusEffect : ForceHealStatusEffectBase
    {
        public override string Name => "Force Heal II";
        protected override int Amount => 20;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceHeal3StatusEffect),
            typeof(ForceHeal4StatusEffect),
            typeof(ForceHeal5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceHeal1StatusEffect)
        };
    }
}
