using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSpark1StatusEffect : ForceSparkStatusEffectBase
    {
        public ForceSpark1StatusEffect()
            : base(2)
        {
        }

        public override string Name => "Force Spark I";

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceSpark2StatusEffect),
            typeof(ForceSpark3StatusEffect)
        };
    }
}
