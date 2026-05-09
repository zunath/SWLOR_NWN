using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Disturbance1StatusEffect : DisturbanceStatusEffectBase
    {
        public Disturbance1StatusEffect()
            : base(2)
        {
        }

        public override string Name => "Disturbance I";

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(Disturbance2StatusEffect),
            typeof(Disturbance3StatusEffect)
        };
    }
}
