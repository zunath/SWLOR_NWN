using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceHeal5StatusEffect : ForceHealStatusEffectBase
    {
        public override string Name => "Force Heal V";
        protected override int Amount => 50;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceHeal1StatusEffect),
            typeof(ForceHeal2StatusEffect),
            typeof(ForceHeal3StatusEffect),
            typeof(ForceHeal4StatusEffect)
        };
    }
}
