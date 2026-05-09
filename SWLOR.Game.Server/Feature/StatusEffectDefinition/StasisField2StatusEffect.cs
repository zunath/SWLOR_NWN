using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class StasisField2StatusEffect : StasisFieldStatusEffectBase
    {
        protected override int DefenseBonus => 4;

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(StasisField3StatusEffect)
        };

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(StasisField1StatusEffect)
        };
    }
}
