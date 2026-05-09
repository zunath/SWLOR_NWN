using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Disturbance3StatusEffect : DisturbanceStatusEffectBase
    {
        public Disturbance3StatusEffect()
            : base(6)
        {
        }

        public override string Name => "Disturbance III";

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(Disturbance1StatusEffect),
            typeof(Disturbance2StatusEffect)
        };
    }
}
