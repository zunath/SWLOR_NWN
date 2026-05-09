using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class StasisField3StatusEffect : StasisFieldStatusEffectBase
    {
        protected override int DefenseBonus => 6;

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(StasisField1StatusEffect),
            typeof(StasisField2StatusEffect)
        };
    }
}
