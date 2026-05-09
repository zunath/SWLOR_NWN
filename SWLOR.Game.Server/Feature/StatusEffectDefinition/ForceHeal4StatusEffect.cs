using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceHeal4StatusEffect : ForceHealStatusEffectBase
    {
        public override string Name => "Force Heal IV";
        protected override int Amount => 40;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceHeal5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceHeal1StatusEffect),
            typeof(ForceHeal2StatusEffect),
            typeof(ForceHeal3StatusEffect)
        };
    }
}
