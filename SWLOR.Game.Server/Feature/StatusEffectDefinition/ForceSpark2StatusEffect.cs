using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSpark2StatusEffect : ForceSparkStatusEffectBase
    {
        public ForceSpark2StatusEffect()
            : base(4)
        {
        }

        public override string Name => "Force Spark II";

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceSpark3StatusEffect)
        };

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceSpark1StatusEffect)
        };
    }
}
