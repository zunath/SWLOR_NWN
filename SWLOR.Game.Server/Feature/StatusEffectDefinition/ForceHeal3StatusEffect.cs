using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceHeal3StatusEffect : ForceHealStatusEffectBase
    {
        public override string Name => "Force Heal III";
        protected override int Amount => 30;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceHeal4StatusEffect),
            typeof(ForceHeal5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceHeal1StatusEffect),
            typeof(ForceHeal2StatusEffect)
        };
    }
}
