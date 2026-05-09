using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class StasisField1StatusEffect : StasisFieldStatusEffectBase
    {
        protected override int DefenseBonus => 2;

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(StasisField2StatusEffect),
            typeof(StasisField3StatusEffect)
        };
    }
}
