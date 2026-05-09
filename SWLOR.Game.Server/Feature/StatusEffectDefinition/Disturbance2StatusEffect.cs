using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Disturbance2StatusEffect : DisturbanceStatusEffectBase
    {
        public Disturbance2StatusEffect()
            : base(4)
        {
        }

        public override string Name => "Disturbance II";

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(Disturbance3StatusEffect)
        };

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(Disturbance1StatusEffect)
        };
    }
}
